using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService;
using Xunit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveFansMedalTaskTests
{
    private static ActivatedMedalResponse Tasks(
        string action,
        string title,
        string progress,
        bool lighted = true
    ) =>
        new()
        {
            Is_lighted = lighted,
            Task_info =
            [
                new()
                {
                    Jump_type = action,
                    Title = title,
                    Sub_title = progress,
                },
            ],
        };

    [Theory]
    [InlineData("like", "点赞30次", "每日上限 7/10", 90)]
    [InlineData("sendDanmu", "发送弹幕1次", "每日上限 3/8", 5)]
    [InlineData("watchLive", "观看直播15分钟", "每日上限 7.5/10", 2250)]
    [InlineData("watchLive", "观看直播5分钟", "每日上限 1/6", 1500)]
    [InlineData("like", "点赞30次", "每日上限 10/10", 0)]
    public void Planner_UsesServerRequirementsAndRemainingProgress(
        string action,
        string title,
        string progress,
        int expected
    ) =>
        Assert.Equal(
            expected,
            LiveFansMedalTaskPlanner.Plan(Tasks(action, title, progress), action).Remaining
        );

    [Theory]
    [InlineData("like", 90)]
    [InlineData("sendDanmu", 5)]
    [InlineData("watchLive", 75)]
    public async Task AutoRunnerUsesRemainingQuotaInsteadOfLegacyCustomNumbers(
        string action,
        int expected
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.FollowDailyTaskLimit = false;
        env.Options.LikeNumber = env.Options.SendDanmakuNumber = env.Options.HeartBeatNumber = 1;
        env.TaskData = (_, _) =>
            action switch
            {
                "like" => Tasks(action, "点赞30次", $"每日上限 {7 + env.Likes.Sum() / 30}/10"),
                "sendDanmu" => Tasks(action, "弹幕1次", $"每日上限 {3 + env.Danmaku}/8"),
                _ => Tasks(
                    action,
                    "观看15分钟",
                    $"每日上限 {7.5m + env.Heartbeats.Count / 30m}/10"
                ),
            };
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        Assert.Equal(
            expected,
            action == "like" ? env.Likes.Sum()
                : action == "sendDanmu" ? env.Danmaku
                : env.Heartbeats.Count
        );
        Assert.Equal(0, env.Pages);
    }

    [Fact]
    public async Task DefaultDanmakuBudgetCompletesTenMessageTaskAndDoesNotRepeat()
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.TaskData = (_, _) => Tasks("sendDanmu", "发弹幕", $"每日上限 {env.Danmaku}/10");
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "sendDanmu");
        Assert.Equal(10, env.Danmaku);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "sendDanmu");
        Assert.Equal(10, env.Danmaku);
    }

    [Theory]
    [InlineData("like", 13)]
    [InlineData("sendDanmu", 2)]
    [InlineData("watchLive", 4)]
    public async Task ConfiguredDailyLimitsAreEditableAndDoNotRepeatOnLaterPolls(
        string action,
        int expected
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.FollowDailyTaskLimit = true;
        env.Options.DailyLikeNumber = 13;
        env.Options.DailyDanmakuNumber = 2;
        env.Options.DailyWatchMinutes = 2;
        env.TaskData = (_, _) =>
            action switch
            {
                "like" => Tasks(action, "点赞30次", $"每日上限 {env.Likes.Sum() / 30}/10"),
                "sendDanmu" => Tasks(action, "弹幕1次", $"每日上限 {env.Danmaku}/8"),
                _ => Tasks(action, "观看15分钟", $"每日上限 {env.Heartbeats.Count / 30m}/10"),
            };
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        Assert.Equal(
            expected,
            action == "like" ? env.Likes.Sum()
                : action == "sendDanmu" ? env.Danmaku
                : env.Heartbeats.Count
        );
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(223, 13)]
    [InlineData(500, 90)]
    public async Task ConfiguredLimitsCountExistingPlatformProgressAndStopAtPlatformCompletion(
        int configured,
        int expected
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyLikeNumber = configured;
        env.TaskData = (_, _) =>
            Tasks("like", "点赞30次", $"每日上限 {7 + env.Likes.Sum() / 30}/10");
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "like");
        Assert.Equal(expected, env.Likes.Sum());
    }

    [Fact]
    public void DailyLimitsPersistAcrossRestartKeepPartialRoundsAndResetOnChinaDate()
    {
        using var directory = new TemporaryBudgetDirectory();
        var clock = new BudgetClock();
        var path = Path.Combine(directory.Path, "usage.json");
        var gate = new LiveFansMedalExecutionGate(clock, path);
        Assert.Equal(13, gate.Remaining("1", 60, "like", 223, 210));
        Assert.Equal(13, gate.Reserve("1", 60, "like", 223, 13));
        var reopened = new LiveFansMedalExecutionGate(clock, path);
        Assert.Equal(0, reopened.Remaining("1", 60, "like", 223, 210));
        Assert.Equal(17, reopened.Remaining("1", 60, "like", 240, 210));
        Assert.Equal(223, reopened.Remaining("2", 60, "like", 223));
        Assert.Equal(223, reopened.Remaining("1", 61, "like", 223));
        clock.Now = clock.Now.AddMinutes(2);
        Assert.Equal(223, reopened.Remaining("1", 60, "like", 223));
    }

    public sealed class BudgetClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 6, 23, 59, 0, TimeSpan.FromHours(8));

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class TemporaryBudgetDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory(
            "medal-budget-"
        );
        public string Path => _directory.FullName;

        public void Dispose() => _directory.Delete(true);
    }

    [Fact]
    public async Task SharedExecutionGatePreventsManualAndMonitoredDuplicateInteractionsAndReleasesAfterCancellation()
    {
        var env = new Environment();
        env.TaskData = (_, _) => Tasks("like", "点赞30次", $"每日上限 {env.Likes.Sum() / 30}/10");
        var gate = new LiveFansMedalExecutionGate();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancel = new CancellationTokenSource();
        var monitored = new LiveFansMedalTaskRunner(
            env.Api,
            env.Trace,
            NullLogger.Instance,
            env.Options,
            "offline-test",
            async (_, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            },
            gate
        );
        var manual = new LiveFansMedalTaskRunner(
            env.Api,
            env.Trace,
            NullLogger.Instance,
            env.Options,
            "offline-test",
            (_, _) => Task.CompletedTask,
            gate
        );
        var running = monitored.RunForAnchorAsync(env.Cookie, 60, 1060, "like", cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await manual.RunAsync(env.Cookie, "like");
        Assert.Equal(10, env.Likes.Sum());
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        await manual.RunForAnchorAsync(env.Cookie, 60, 1060, "like");
        Assert.Equal(40, env.Likes.Sum());
    }

    [Fact]
    public void MonitoredLikeBudgetIsSharedAcrossAnchorsAndSeparateForAccounts()
    {
        var env = new Environment();
        var gate = new LiveFansMedalExecutionGate();
        Assert.Equal(3000, gate.ReserveMonitoredLikes(env.Cookie, 3000));
        Assert.Equal(2000, gate.ReserveMonitoredLikes(env.Cookie, 3000));
        Assert.Equal(0, gate.ReserveMonitoredLikes(env.Cookie, 10));
        Assert.Equal(
            10,
            gate.ReserveMonitoredLikes(new BiliCookie(new() { ["DedeUserID"] = "2" }), 10)
        );
    }

    [Fact]
    public void Planner_CompletedAndSavingsFullTasksNeedNoInteraction()
    {
        var data = Tasks("like", "点赞30次", "每日上限 0/10");
        data.Task_info[0].Is_done = true;
        Assert.Equal(0, LiveFansMedalTaskPlanner.Plan(data, "like").Remaining);
        data.Task_info[0].Is_done = false;
        data.Reach_free_intimacy_limit = true;
        Assert.Equal(0, LiveFansMedalTaskPlanner.Plan(data, "like").Remaining);
        Assert.Equal(0, LiveFansMedalTaskPlanner.Plan(data, "watchLive").Remaining);
    }

    [Fact]
    public void Planner_UnlitMedalUsesLightingRequirementEvenWhenSavingsFull()
    {
        var data = Tasks("sendDanmu", "发送10条弹幕", "完成即可点亮", false);
        data.Reach_free_intimacy_limit = true;
        Assert.Equal(10, LiveFansMedalTaskPlanner.Plan(data, "sendDanmu").Remaining);
    }

    [Theory]
    [InlineData("点赞若干次", "每日上限 0/10")]
    [InlineData("点赞30次", "新的未知进度")]
    [InlineData("点赞30次", "每日上限 0/100000")]
    public void Planner_ChangedRequirementsAreReported(string title, string progress) =>
        Assert.Throws<InvalidOperationException>(() =>
            LiveFansMedalTaskPlanner.Plan(Tasks("like", title, progress), "like")
        );

    [Fact]
    public void Configuration_OldLevel20SwitchIsIgnoredAndBudgetsRemain()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["LiveFansMedalTaskConfig:UseLiveStateMonitoring"] = "false",
                    ["LiveFansMedalTaskConfig:FollowDailyTaskLimit"] = "false",
                    ["LiveFansMedalTaskConfig:IsSkipLevel20Medal"] = "true",
                    ["LiveFansMedalTaskConfig:HeartBeatNumber"] = "70",
                    ["LiveFansMedalTaskConfig:LikeNumber"] = "30",
                }
            )
            .Build();
        var options = config.GetSection("LiveFansMedalTaskConfig").Get<LiveFansMedalTaskOptions>()!;
        Assert.False(options.FollowDailyTaskLimit);
        Assert.Equal(70, options.HeartBeatNumber);
        Assert.DoesNotContain(
            "LiveFansMedalTaskConfig:IsSkipLevel20Medal",
            options.ToConfigDictionary().Keys
        );
        Assert.Equal("true", options.ToConfigDictionary()["LiveFansMedalTaskConfig:EnableWatch"]);
    }

    [Fact]
    public void ApiPayload_DeserializesCurrentPanelAndTaskFields()
    {
        var panel = JsonSerializer.Deserialize<FansMedalPanelResponse>(
            """
            {"list":[{"medal":{"target_id":321,"level":60},"room_info":{"room_id":987}}],"special_list":[],"page_info":{"total_page":2,"has_more":true}}
            """,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;
        Assert.Equal(60, panel.List[0].Medal.Level);
        Assert.Equal(987, panel.List[0].Room_info.Room_id);
        Assert.True(panel.Page_info.Has_more);
        var data = JsonSerializer.Deserialize<ActivatedMedalResponse>(
            """
            {"is_lighted":true,"reach_free_intimacy_limit":false,"task_info":[{"title":"点赞30次","sub_title":"每日上限 9/10","jump_type":"like","is_done":false}]}
            """,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;
        Assert.Equal(30, LiveFansMedalTaskPlanner.Plan(data, "like").Remaining);
    }

    [Fact]
    public async Task Runner_PaginatesDeduplicatesAndIncludesHighLevelMedals()
    {
        var env = new Environment();
        env.Panel = page =>
            new()
            {
                List = [Medal(20), Medal(21), Medal(60)],
                Special_list = [Medal(60)],
                Page_info = new() { Total_page = 2 },
            };
        env.TaskData = (_, _) => Tasks("like", "点赞30次", "每日上限 10/10");
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(2, env.Pages);
        Assert.Equal(3, env.TaskReads);
        Assert.Empty(env.Likes);
    }

    [Fact]
    public async Task Runner_CustomLikesAreBatchedAndProgressIsRechecked()
    {
        var env = new Environment();
        env.TaskData = (_, _) =>
            Tasks("like", "点赞30次", env.Likes.Sum() >= 30 ? "每日上限 1/10" : "每日上限 0/10");
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(new[] { 10, 10, 10 }, env.Likes);
        Assert.Equal(2, env.TaskReads);
    }

    [Fact]
    public async Task Runner_AutomaticGoalOnlySendsRemainingRounds()
    {
        var env = new Environment();
        env.Options.FollowDailyTaskLimit = true;
        env.TaskData = (_, _) =>
            Tasks("like", "点赞30次", $"每日上限 {8 + env.Likes.Sum() / 30}/10");
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(60, env.Likes.Sum());
        Assert.Equal(3, env.TaskReads);
    }

    [Fact]
    public async Task Runner_AutomaticGoalRefreshesLightingBeforeUpgradeTasks()
    {
        var env = new Environment();
        env.Options.FollowDailyTaskLimit = true;
        env.TaskData = (_, _) =>
            env.Likes.Sum() < 30
                ? Tasks("like", "点赞30次", "完成即可点亮", false)
                : Tasks("like", "点赞30次", $"每日上限 {(env.Likes.Sum() - 30) / 30}/2");
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(90, env.Likes.Sum());
        Assert.Equal(4, env.TaskReads);
    }

    [Fact]
    public async Task Runner_AccountLikeBudgetIsSharedAcrossMedals()
    {
        var env = new Environment();
        env.Options.FollowDailyTaskLimit = true;
        env.Panel = _ => new() { List = [Medal(20), Medal(60)] };
        env.TaskData = (id, _) =>
            Tasks("like", "点赞1000次", $"每日上限 {(id == 20 ? env.Likes.Sum() / 1000 : 0)}/10");
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(5000, env.Likes.Sum());
        Assert.All(env.Likes, amount => Assert.InRange(amount, 1, 10));
    }

    [Fact]
    public async Task Runner_DisabledActionsAndZeroCustomBudgetMakeNoRequests()
    {
        var env = new Environment();
        env.Options.EnableLike = false;
        await env.Runner.RunAsync(env.Cookie, "like");
        env.Options.EnableLike = true;
        env.Options.LikeNumber = 0;
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(0, env.Pages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Runner_OfflineAndRoundPlayRoomsDoNotReceiveLikes(int status)
    {
        var env = new Environment { LiveStatus = status };
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Empty(env.Likes);
        Assert.Equal(0, env.Enters);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    public async Task Runner_OfflineWatchingConfirmsPlatformProgressInBothModes(
        int status,
        bool monitored
    )
    {
        var env = new Environment { LiveStatus = status };
        env.Options.UseLiveStateMonitoring = monitored;
        env.Options.FollowDailyTaskLimit = !monitored;
        env.TaskData = (_, _) =>
        {
            var completed = env.Heartbeats.Count >= 30;
            var data = Tasks(
                "watchLive",
                "观看直播15分钟",
                completed ? "每日上限 10/10" : "每日上限 9/10"
            );
            data.Task_info[0].Is_done = completed;
            return data;
        };
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(1, env.Enters);
        Assert.Equal(30, env.Heartbeats.Count);
        Assert.Equal(1, env.RoomReads);
        Assert.True(env.TaskData(60, 0).Task_info[0].Is_done);
    }

    [Fact]
    public async Task Runner_WatchingContinuesAfterOfflineTransitionWithStableAreaIdentity()
    {
        var env = new Environment();
        env.Options.HeartBeatNumber = 1;
        env.BeforeDelay = () =>
        {
            env.LiveStatus = 0;
            env.AreaId = 9;
            env.ParentAreaId = 8;
        };
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(2, env.Heartbeats.Count);
        Assert.Equal(1, env.RoomReads);
        Assert.All(
            env.Heartbeats,
            beat => Assert.Equal([1L, 1L], JsonSerializer.Deserialize<long[]>(beat.Id)!.Take(2))
        );
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public async Task Runner_WatchingRequiresBothValidAreaIdentifiers(int parent, int area)
    {
        var env = new Environment
        {
            LiveStatus = 0,
            ParentAreaId = parent,
            AreaId = area,
        };
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(0, env.Enters);
        Assert.Empty(env.Heartbeats);
    }

    [Fact]
    public async Task Runner_UnlitMedalDoesNotStartWatching()
    {
        var env = new Environment { LiveStatus = 0 };
        env.TaskData = (_, _) => Tasks("watchLive", "观看15分钟", "每日上限 0/10", false);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(0, env.Enters);
        Assert.Equal(0, env.RoomReads);
    }

    [Fact]
    public async Task Runner_OfflineHeartbeatAcceptanceDoesNotSubstituteForTaskCompletion()
    {
        var env = new Environment { LiveStatus = 0 };
        env.Options.FollowDailyTaskLimit = true;
        env.TaskData = (_, _) => Tasks("watchLive", "观看1分钟", "每日上限 9/10");
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive")
        );
        Assert.Equal(2, env.Heartbeats.Count);
        Assert.True(env.TaskReads >= 3);
        Assert.False(env.TaskData(60, 0).Task_info[0].Is_done);
    }

    [Fact]
    public async Task Runner_OfflineHeartbeatRejectionStopsAtConfiguredFailureLimit()
    {
        var env = new Environment { LiveStatus = 0, HeartbeatCode = -400 };
        env.Options.HeartBeatSendGiveUpThreshold = 2;
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive")
        );
        Assert.Equal(2, env.Heartbeats.Count);
        Assert.Equal(2, env.TaskReads);
    }

    [Fact]
    public async Task Runner_WatchUsesActualIntervalAndStableDeviceWithoutCountingEntry()
    {
        var env = new Environment();
        env.Options.HeartBeatNumber = 1;
        await env.Runner.RunAsync(env.Cookie, "watchLive");
        Assert.Equal(1, env.Enters);
        Assert.Equal(2, env.Heartbeats.Count);
        Assert.All(
            env.Heartbeats,
            beat =>
            {
                Assert.Equal(30, beat.Time);
                Assert.Equal(env.EntryDevice, beat.Device);
            }
        );
        Assert.Equal(
            new[] { 1, 2 },
            env.Heartbeats.Select(beat => JsonSerializer.Deserialize<long[]>(beat.Id)![2])
                .Select(value => (int)value)
        );
        Assert.Equal(60, env.Delays.Sum(value => value.TotalSeconds));
    }

    [Fact]
    public async Task WatchHeartbeatKeepsServerScheduleDespiteResponseAndProgressLatency()
    {
        var env = new Environment
        {
            HeartbeatResponseDelay = TimeSpan.FromSeconds(2),
            TaskReadDelay = TimeSpan.FromSeconds(7),
        };
        env.Options.HeartBeatNumber = 15;
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(30, env.Heartbeats.Count);
        Assert.Equal(1, env.Enters);
        Assert.All(env.Heartbeats, beat => Assert.Equal((beat.Ets + beat.Time) * 1000, beat.Ts));
        Assert.Equal(
            1,
            env.Heartbeats.Select(beat => beat.Ts - beat.Ets * 1000).Distinct().Count()
        );
        Assert.Contains(env.Delays, wait => wait < TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task RejectedHeartbeatRenewsSessionAndOnlyAcceptedTimeConsumesDailyBudget()
    {
        using var directory = new TemporaryBudgetDirectory();
        var env = new Environment { HeartbeatCodeAt = n => n == 1 ? 1012002 : 0 };
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyWatchMinutes = 1;
        var path = Path.Combine(directory.Path, "usage.json");
        var gate = new LiveFansMedalExecutionGate(env.Clock, path);
        var runner = new LiveFansMedalTaskRunner(
            env.Api,
            env.Trace,
            NullLogger.Instance,
            env.Options,
            "synthetic",
            env.DelayAsync,
            gate,
            clock: env.Clock
        );
        await runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(2, env.Enters);
        Assert.Equal(3, env.Heartbeats.Count);
        Assert.Equal(
            new long[] { 1, 1, 2 },
            env.Heartbeats.Select(beat => JsonSerializer.Deserialize<long[]>(beat.Id)![2])
        );
        var reopened = new LiveFansMedalExecutionGate(env.Clock, path);
        Assert.Equal(0, reopened.Remaining("1", 60, "watchLive", 60));
        Assert.Equal(30, reopened.Remaining("1", 60, "watchLive", 90));
    }

    [Fact]
    public async Task RepeatedRejectedHeartbeatsPreserveWatchBudgetAndStopAtFailureLimit()
    {
        using var directory = new TemporaryBudgetDirectory();
        var env = new Environment { HeartbeatCode = 1012002 };
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyWatchMinutes = 1;
        env.Options.HeartBeatSendGiveUpThreshold = 2;
        var path = Path.Combine(directory.Path, "usage.json");
        var gate = new LiveFansMedalExecutionGate(env.Clock, path);
        var runner = new LiveFansMedalTaskRunner(
            env.Api,
            env.Trace,
            NullLogger.Instance,
            env.Options,
            "synthetic",
            env.DelayAsync,
            gate,
            clock: env.Clock
        );
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive")
        );
        Assert.Equal(2, env.Heartbeats.Count);
        Assert.Equal(2, env.Enters);
        Assert.Equal(
            60,
            new LiveFansMedalExecutionGate(env.Clock, path).Remaining("1", 60, "watchLive", 60)
        );
    }

    [Fact]
    public async Task OversleptHeartbeatRenewsSessionBeforeSendingAnyStaleRequest()
    {
        var env = new Environment();
        env.Options.HeartBeatNumber = 1;
        var first = true;
        env.BeforeDelay = () =>
        {
            if (!first)
                return;
            first = false;
            env.Clock.Now = env.Clock.Now.AddSeconds(12);
        };
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(2, env.Enters);
        Assert.Equal(2, env.Heartbeats.Count);
        Assert.All(env.Heartbeats, beat => Assert.Equal((beat.Ets + beat.Time) * 1000, beat.Ts));
    }

    [Fact]
    public async Task Runner_WatchStopsWhenServerTaskIsCompleted()
    {
        var env = new Environment();
        env.Options.FollowDailyTaskLimit = true;
        env.TaskData = (_, _) =>
            Tasks(
                "watchLive",
                "观看直播1分钟",
                env.Heartbeats.Count >= 2 ? "每日上限 10/10" : "每日上限 9/10"
            );
        await env.Runner.RunAsync(env.Cookie, "watchLive");
        Assert.Equal(2, env.Heartbeats.Count);
    }

    [Fact]
    public async Task Runner_StillPendingTaskDoesNotClaimCompletion()
    {
        var env = new Environment();
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            env.Runner.RunAsync(env.Cookie, "like")
        );
        Assert.Equal(30, env.Likes.Sum());
    }

    [Fact]
    public async Task Runner_TaskReadFailureIsReportedWithoutPerformingActivities()
    {
        var env = new Environment { TaskCode = -101 };
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            env.Runner.RunAsync(env.Cookie, "like")
        );
        Assert.Empty(env.Likes);
    }

    [Fact]
    public async Task Runner_OfflineOnlyDanmakuAndLightingPreferencesAreRespected()
    {
        var env = new Environment();
        env.Options.DanmakuOnlyWhenOffline = true;
        await env.Runner.RunAsync(env.Cookie, "sendDanmu");
        Assert.Equal(0, env.Danmaku);
        env.LiveStatus = 0;
        env.TaskData = (_, _) => Tasks("sendDanmu", "发送弹幕1次", $"每日上限 {env.Danmaku}/10");
        await env.Runner.RunAsync(env.Cookie, "sendDanmu");
        Assert.Equal(1, env.Danmaku);
        env.LiveStatus = 1;
        env.Options.DanmakuOnlyWhenOffline = false;
        env.TaskData = (_, _) => Tasks("sendDanmu", "发送10条弹幕", "完成即可点亮", false);
        await env.Runner.RunAsync(env.Cookie, "sendDanmu");
        Assert.Equal(1, env.Danmaku);
    }

    [Fact]
    public async Task Runner_CancellationInterruptsWatchBeforeSendingHeartbeat()
    {
        var env = new Environment();
        using var cancellation = new CancellationTokenSource();
        var runner = new LiveFansMedalTaskRunner(
            env.Api,
            env.Trace,
            NullLogger.Instance,
            env.Options,
            "offline-test",
            (_, token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(token);
            },
            clock: env.Clock
        );
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runner.RunAsync(env.Cookie, "watchLive", cancellation.Token)
        );
        Assert.Empty(env.Heartbeats);
    }

    private static FansMedalPanelItem Medal(int level) =>
        new()
        {
            Medal = new()
            {
                Target_id = level,
                Medal_id = level,
                Level = level,
                Medal_name = "Test medal",
            },
            Room_info = new() { Room_id = level },
        };

    [Theory]
    [InlineData("like")]
    [InlineData("sendDanmu")]
    [InlineData("watchLive")]
    public async Task Runner_ExcludedAnchorsAreSkippedBeforeTaskReadsAndActivities(string action)
    {
        var env = new Environment();
        env.Options.ExcludedAnchorIds = "60,60,999";
        env.Panel = _ => new() { List = [Medal(60)], Special_list = [Medal(60)] };
        await env.Runner.RunAsync(env.Cookie, action);
        Assert.Equal(0, env.TaskReads);
        Assert.Empty(env.Likes);
        Assert.Equal(0, env.Danmaku);
        Assert.Equal(0, env.Enters);
        Assert.Empty(env.Heartbeats);
    }

    [Fact]
    public async Task Runner_ExclusionKeepsOtherAnchorsAndPersistsAsConfiguration()
    {
        var env = new Environment();
        env.Options.ExcludedAnchorIds = "20,999";
        env.Panel = _ => new() { List = [Medal(20), Medal(60)] };
        env.TaskData = (id, _) =>
        {
            Assert.Equal(60, id);
            return Tasks("like", "点赞30次", "每日上限 10/10");
        };
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(1, env.TaskReads);
        Assert.Equal(
            "20,999",
            env.Options.ToConfigDictionary()["LiveFansMedalTaskConfig:ExcludedAnchorIds"]
        );
    }

    [Theory]
    [InlineData("like", "", "")]
    [InlineData("sendDanmu", "", "")]
    [InlineData("watchLive", "", "")]
    [InlineData("like", "60", "60")]
    [InlineData("sendDanmu", "60", "60")]
    [InlineData("watchLive", "60", "60")]
    public async Task Runner_EmptyWhitelistOrExcludedSelection_PerformsNoTaskReadsOrActivities(
        string action,
        string included,
        string excluded
    )
    {
        var env = new Environment();
        env.Options.OnlySelectedAnchors = true;
        env.Options.IncludedAnchorIds = included;
        env.Options.ExcludedAnchorIds = excluded;
        await env.Runner.RunAsync(env.Cookie, action);
        Assert.Equal(0, env.TaskReads);
        Assert.Empty(env.Likes);
    }

    [Fact]
    public async Task Runner_WhitelistReadsOnlySelectedAnchorAndPersistsSettings()
    {
        var env = new Environment();
        env.Options.OnlySelectedAnchors = true;
        env.Options.IncludedAnchorIds = "60,999";
        env.Panel = _ => new() { List = [Medal(20), Medal(60)] };
        env.TaskData = (_, _) => Tasks("like", "点赞30次", "每日上限 10/10");
        await env.Runner.RunAsync(env.Cookie, "like");
        Assert.Equal(1, env.TaskReads);
        Assert.Equal(
            "true",
            env.Options.ToConfigDictionary()["LiveFansMedalTaskConfig:OnlySelectedAnchors"]
        );
        Assert.Equal(
            "60,999",
            env.Options.ToConfigDictionary()["LiveFansMedalTaskConfig:IncludedAnchorIds"]
        );
    }

    [Fact]
    public async Task RecoveryReportsAcceptedWatchTimeSeparatelyFromConfirmedPlatformProgress()
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyWatchMinutes = 2;
        env.TaskData = (_, _) => Tasks("watchLive", "观看15分钟", "每日上限 0/10");
        var entries = new List<Ray.BiliBiliTool.Domain.TaskRecoveryProgress>();
        using var scope = new Ray.BiliBiliTool.Domain.TaskRecoveryProgressScope(entries.Add);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        var watch = entries.Where(entry => entry.Key.EndsWith("/medal/60/watchLive")).ToArray();
        Assert.Contains(watch, entry => entry.Current == 30 && entry.Total == 120);
        Assert.Contains(
            watch,
            entry => entry.State == Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Waiting
        );
        Assert.Equal(120, watch.Last().Current);
        Assert.Equal(Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Pending, watch.Last().State);
        Assert.Contains("0/10", watch.Last().PlatformProgress);
        Assert.DoesNotContain(
            watch,
            entry => entry.State == Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Completed
        );
    }

    [Theory]
    [InlineData("like", "未开播")]
    [InlineData("sendDanmu", "等待主播下播")]
    public async Task RecoveryReportsRoomConditionRatherThanStayingRunning(
        string action,
        string reason
    )
    {
        var env = new Environment { LiveStatus = action == "like" ? 0 : 1 };
        env.Options.DanmakuOnlyWhenOffline = true;
        var entries = new List<Ray.BiliBiliTool.Domain.TaskRecoveryProgress>();
        using var scope = new Ray.BiliBiliTool.Domain.TaskRecoveryProgressScope(entries.Add);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        var last = entries.Last(entry => entry.Key.EndsWith($"/medal/60/{action}"));
        Assert.Equal(Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Pending, last.State);
        Assert.Contains(reason, last.Detail);
        Assert.Empty(env.Likes);
        Assert.Equal(0, env.Danmaku);
    }

    [Fact]
    public async Task RecoveryPreservesSpecificHeartbeatFailureAndRetryCount()
    {
        var env = new Environment { HeartbeatCode = 1012002 };
        env.Options.HeartBeatSendGiveUpThreshold = 2;
        var entries = new List<Ray.BiliBiliTool.Domain.TaskRecoveryProgress>();
        using var scope = new Ray.BiliBiliTool.Domain.TaskRecoveryProgressScope(entries.Add);
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive")
        );
        var last = entries.Last(entry => entry.Key.EndsWith("/medal/60/watchLive"));
        Assert.Equal(Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Failed, last.State);
        Assert.Contains("1012002", last.Detail);
        Assert.Contains("重试 2 次", last.Detail);
    }

    [Theory]
    [InlineData("like", 10)]
    [InlineData("sendDanmu", 1)]
    public async Task RejectedInteractionsRefundPersistedBudgetAndAllowRecovery(
        string action,
        int limit
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyLikeNumber = env.Options.DailyDanmakuNumber = limit;
        env.InteractionCodeAt = _ => -400;
        env.TaskData = (_, _) =>
            Tasks(
                action,
                action == "like" ? "点赞10次" : "发弹幕1次",
                $"每日上限 {(action == "like" ? env.Likes.Sum() / 10 : env.Danmaku)}/1"
            );
        var path = Path.Combine(Path.GetTempPath(), $"refunded-budget-{Guid.NewGuid():N}.json");
        try
        {
            var gate = new LiveFansMedalExecutionGate(env.Clock, path);
            var runner = new LiveFansMedalTaskRunner(
                env.Api,
                env.Trace,
                NullLogger.Instance,
                env.Options,
                "synthetic",
                env.DelayAsync,
                gate,
                clock: env.Clock
            );
            await Assert.ThrowsAsync<BiliBusinessException>(() =>
                runner.RunForAnchorAsync(env.Cookie, 60, 1060, action)
            );
            Assert.Equal(3, env.InteractionAttempts);
            Assert.Equal(limit, gate.Remaining(env.Cookie.UserId, 60, action, limit));
            var reopened = new LiveFansMedalExecutionGate(env.Clock, path);
            Assert.Equal(limit, reopened.Remaining(env.Cookie.UserId, 60, action, limit));
            env.InteractionCodeAt = _ => 0;
            var recovery = new LiveFansMedalTaskRunner(
                env.Api,
                env.Trace,
                NullLogger.Instance,
                env.Options,
                "synthetic",
                env.DelayAsync,
                reopened,
                clock: env.Clock
            );
            await recovery.RunForAnchorAsync(env.Cookie, 60, 1060, action);
            Assert.Equal(4, env.InteractionAttempts);
            Assert.Equal(0, reopened.Remaining(env.Cookie.UserId, 60, action, limit));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void InteractionReservationUsesActualAnchorAmountAndRefundsAccountCap()
    {
        var env = new Environment();
        var gate = new LiveFansMedalExecutionGate(env.Clock);
        Assert.Equal(4995, gate.ReserveMonitoredLikes(env.Cookie, 4995));
        Assert.Equal(1, gate.ReserveInteraction(env.Cookie, 60, "like", 1, 10, out var date));
        Assert.Equal(4, gate.ReserveMonitoredLikes(env.Cookie, 10));
        gate.ReleaseRejectedInteraction(env.Cookie, 60, "like", 1, date);
        Assert.Equal(1, gate.ReserveMonitoredLikes(env.Cookie, 10));
        Assert.Equal(1, gate.Remaining(env.Cookie.UserId, 60, "like", 1));
    }

    [Fact]
    public void OldRefundDoesNotConsumeOrReleaseNextDayBudget()
    {
        var env = new Environment();
        var gate = new LiveFansMedalExecutionGate(env.Clock);
        Assert.Equal(10, gate.ReserveInteraction(env.Cookie, 60, "like", 10, 10, out var oldDate));
        env.Clock.Now = env.Clock.Now.AddDays(1);
        Assert.Equal(10, gate.ReserveInteraction(env.Cookie, 60, "like", 10, 10, out _));
        gate.ReleaseRejectedInteraction(env.Cookie, 60, "like", 10, oldDate);
        Assert.Equal(0, gate.Remaining(env.Cookie.UserId, 60, "like", 10));
        Assert.Equal(4990, gate.ReserveMonitoredLikes(env.Cookie, 5000));
    }

    [Fact]
    public async Task UnknownTransportOutcomeRetainsReservation()
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyLikeNumber = 10;
        env.TaskData = (_, _) => Tasks("like", "点赞10次", "每日上限 0/1");
        env.InteractionCodeAt = _ => throw new HttpRequestException("synthetic timeout");
        var gate = new LiveFansMedalExecutionGate(env.Clock);
        var runner = new LiveFansMedalTaskRunner(
            env.Api,
            env.Trace,
            NullLogger.Instance,
            env.Options,
            "synthetic",
            env.DelayAsync,
            gate,
            clock: env.Clock
        );
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            runner.RunForAnchorAsync(env.Cookie, 60, 1060, "like")
        );
        Assert.Equal(0, gate.Remaining(env.Cookie.UserId, 60, "like", 10));
        Assert.Equal(1, env.InteractionAttempts);
    }

    [Theory]
    [InlineData("like", 10)]
    [InlineData("sendDanmu", 1)]
    [InlineData("watchLive", 2)]
    public async Task RepeatedMedalRecoveryOnlySendsIncompleteAnchorActions(
        string action,
        int expected
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Panel = _ => new() { List = [Medal(60), Medal(61)] };
        env.TaskData = (anchor, _) =>
        {
            var sent =
                action == "like" ? env.Likes.Sum() / 10
                : action == "sendDanmu" ? env.Danmaku
                : env.Heartbeats.Count / 2;
            return Tasks(
                action,
                action == "like" ? "点赞10次"
                    : action == "sendDanmu" ? "发弹幕"
                    : "观看1分钟",
                $"每日上限 {(anchor == 60 ? 1 : sent)}/1"
            );
        };
        var updates = new List<Ray.BiliBiliTool.Domain.TaskRecoveryProgress>();
        using var scope = new Ray.BiliBiliTool.Domain.TaskRecoveryProgressScope(updates.Add);
        await env.Runner.RunAsync(env.Cookie, action);
        var first =
            action == "like" ? env.Likes.Sum()
            : action == "sendDanmu" ? env.Danmaku
            : env.Heartbeats.Count;
        Assert.Equal(expected, first);
        await env.Runner.RunAsync(env.Cookie, action);
        Assert.Equal(
            first,
            action == "like" ? env.Likes.Sum()
                : action == "sendDanmu" ? env.Danmaku
                : env.Heartbeats.Count
        );
        Assert.Contains(
            updates,
            update =>
                update.Key.EndsWith($"/medal/60/{action}")
                && update.State == Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Completed
        );
        Assert.Contains(
            updates,
            update =>
                update.Key.EndsWith($"/medal/61/{action}")
                && update.State == Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Completed
        );
        Assert.DoesNotContain(
            updates,
            update =>
                update.Key.Contains("/medal/")
                && update.State == Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Completed
                && update.PlatformProgress?.Contains("未完成") == true
        );
    }

    [Theory]
    [InlineData("like", "7 次")]
    [InlineData("sendDanmu", "2 次")]
    [InlineData("watchLive", "1 分钟")]
    public async Task ExhaustedBudgetMessageNamesPanelLimitAndConfiguredQuantity(
        string action,
        string quantity
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyLikeNumber = 7;
        env.Options.DailyDanmakuNumber = 2;
        env.Options.DailyWatchMinutes = 1;
        env.TaskData = (_, _) =>
            Tasks(
                action,
                action == "like" ? "点赞10次"
                    : action == "sendDanmu" ? "发弹幕"
                    : "观看1分钟",
                action == "sendDanmu" ? "每日上限 2/10" : "每日上限 1/10"
            );
        var updates = new List<Ray.BiliBiliTool.Domain.TaskRecoveryProgress>();
        using var scope = new Ray.BiliBiliTool.Domain.TaskRecoveryProgressScope(updates.Add);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        var entry = updates.Last(update => update.Key.EndsWith($"/medal/60/{action}"));
        Assert.Equal(Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Pending, entry.State);
        Assert.Contains("面板设置的每日", entry.Detail);
        Assert.Contains(quantity, entry.Detail);
        Assert.Contains("互动设置", entry.Detail);
        Assert.Empty(env.Likes);
        Assert.Equal(0, env.Danmaku);
        Assert.Empty(env.Heartbeats);
    }

    [Fact]
    public async Task WatchRechecksPlatformCompletionBeforeStartingItsSession()
    {
        var env = new Environment();
        env.TaskData = (_, read) =>
            Tasks("watchLive", "观看1分钟", $"每日上限 {(read > 1 ? 1 : 0)}/1");
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(0, env.Enters);
        Assert.Empty(env.Heartbeats);
    }

    [Fact]
    public async Task WatchRechecksPartialProgressAndOnlySendsRemainingConfiguredTime()
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyWatchMinutes = 2;
        env.TaskData = (_, read) =>
            Tasks(
                "watchLive",
                "观看1分钟",
                $"每日上限 {(read > 1 ? 1 + env.Heartbeats.Count / 2 : 0)}/10"
            );
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
        Assert.Equal(2, env.Heartbeats.Count);
    }

    [Theory]
    [InlineData("like", 10)]
    [InlineData("sendDanmu", 1)]
    public async Task AcceptedButUnconfirmedActionsShowPendingAndCanRetryWithoutRepeatingConfirmedActions(
        string action,
        int amount
    )
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyLikeNumber = 10;
        env.Options.DailyDanmakuNumber = 1;
        var complete = false;
        env.TaskData = (_, _) =>
            Tasks(
                action,
                action == "like" ? "点赞10次" : "发弹幕",
                $"每日上限 {(complete ? 1 : 0)}/1"
            );
        var updates = new List<Ray.BiliBiliTool.Domain.TaskRecoveryProgress>();
        using var scope = new Ray.BiliBiliTool.Domain.TaskRecoveryProgressScope(updates.Add);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        var entry = updates.Last(update => update.Key.EndsWith($"/medal/60/{action}"));
        Assert.Equal(Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Pending, entry.State);
        Assert.Contains("等待 B 站确认", entry.Detail);
        Assert.Contains($"今日已确认 0 / {amount}", entry.Detail);
        Assert.DoesNotContain("已用完", entry.Detail);
        Assert.Equal(amount, action == "like" ? env.Likes.Sum() : env.Danmaku);
        env.Clock.Now = env.Clock.Now.AddMinutes(6);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        Assert.Equal(amount * 2, action == "like" ? env.Likes.Sum() : env.Danmaku);
        complete = true;
        env.Clock.Now = env.Clock.Now.AddMinutes(6);
        await env.Runner.RunForAnchorAsync(env.Cookie, 60, 1060, action);
        Assert.Equal(amount * 2, action == "like" ? env.Likes.Sum() : env.Danmaku);
        Assert.Equal(
            Ray.BiliBiliTool.Domain.TaskRecoveryProgressState.Completed,
            updates.Last(update => update.Key.EndsWith($"/medal/60/{action}")).State
        );
    }

    [Fact]
    public async Task WatchDiagnosticsKeepAcceptedTimeSeparateAndDoNotAddApiCalls()
    {
        var env = new Environment();
        env.Options.UseLiveStateMonitoring = true;
        env.Options.DailyWatchMinutes = 32;
        env.TaskData = (_, _) => Tasks("watchLive", "观看15分钟", "每日上限 2/10");
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var diagnostics = new LiveWatchDiagnostics(path, env.Clock);
            var runner = new LiveFansMedalTaskRunner(
                env.Api,
                env.Trace,
                NullLogger.Instance,
                env.Options,
                "offline-test",
                env.DelayAsync,
                clock: env.Clock,
                watchDiagnostics: diagnostics
            );
            await runner.RunForAnchorAsync(env.Cookie, 60, 1060, "watchLive");
            var samples = Directory
                .GetFiles(path, "watch-*.jsonl")
                .SelectMany(File.ReadLines)
                .Select(line =>
                    System.Text.Json.JsonSerializer.Deserialize<LiveWatchDiagnostics.Sample>(line)!
                )
                .ToArray();
            var end = samples.Last();
            Assert.Equal("end", end.Event);
            Assert.Equal(120, end.SentSeconds);
            Assert.Equal(1800, end.InitialConfirmedSeconds);
            Assert.Equal(1800, end.ConfirmedSeconds);
            Assert.False(end.TaskDone);
            Assert.Equal("pending", end.Outcome);
            Assert.Equal(4, samples.Count(sample => sample.Event == "heartbeat"));
            Assert.Equal(4, env.TaskReads);
            var text = string.Join(
                '\n',
                Directory.GetFiles(path, "watch-*.jsonl").SelectMany(File.ReadLines)
            );
            Assert.DoesNotContain(env.Cookie.ToString(), text);
            Assert.DoesNotContain("offline-test", text);
            Assert.DoesNotContain("synthetic", text);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private class Environment
    {
        public LiveFansMedalTaskOptions Options { get; } =
            new() { UseLiveStateMonitoring = false, FollowDailyTaskLimit = false };
        public BiliCookie Cookie { get; } =
            new(
                new()
                {
                    ["DedeUserID"] = "1",
                    ["bili_jct"] = "synthetic",
                    ["LIVE_BUVID"] = "synthetic",
                }
            );
        public ILiveApi Api { get; }
        public ILiveTraceApi Trace { get; }
        public LiveFansMedalTaskRunner Runner { get; }
        public BudgetClock Clock { get; } =
            new() { Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero) };
        public List<int> Likes { get; } = [];
        public List<HeartBeatRequest> Heartbeats { get; } = [];
        public List<TimeSpan> Delays { get; } = [];
        public int LiveStatus { get; set; } = 1;
        public int ParentAreaId { get; set; } = 1;
        public int AreaId { get; set; } = 1;
        public int RoomReads { get; set; }
        public int HeartbeatCode { get; set; }
        public Func<int, int>? HeartbeatCodeAt { get; set; }
        public TimeSpan HeartbeatResponseDelay { get; set; }
        public TimeSpan TaskReadDelay { get; set; }
        public Action? BeforeDelay { get; set; }
        public int TaskCode { get; set; }
        public int Pages { get; set; }
        public int TaskReads { get; set; }
        public int Enters { get; set; }
        public int Danmaku { get; set; }
        public int InteractionAttempts { get; set; }
        public Func<int, int>? InteractionCodeAt { get; set; }
        public string EntryDevice { get; set; } = "";
        public Func<int, FansMedalPanelResponse> Panel { get; set; } =
            _ => new() { List = [Medal(60)] };
        public Func<long, int, ActivatedMedalResponse> TaskData { get; set; } =
            (_, _) =>
                new()
                {
                    Is_lighted = true,
                    Task_info =
                    [
                        new()
                        {
                            Jump_type = "like",
                            Title = "点赞30次",
                            Sub_title = "每日上限 0/10",
                        },
                        new()
                        {
                            Jump_type = "sendDanmu",
                            Title = "发送弹幕1次",
                            Sub_title = "每日上限 0/10",
                        },
                        new()
                        {
                            Jump_type = "watchLive",
                            Title = "观看直播15分钟",
                            Sub_title = "每日上限 0/10",
                        },
                    ],
                };

        public Environment()
        {
            Api = Proxy.Create<ILiveApi>(
                (method, args) =>
                    method switch
                    {
                        "GetFansMedalPanel" => Task.FromResult(
                            new BiliApiResponse<FansMedalPanelResponse>
                            {
                                Code = 0,
                                Data = Panel((int)args[0]!),
                            }
                        ),
                        "GetActivatedMedalInfo" => Task.FromResult(
                            new BiliApiResponse<ActivatedMedalResponse>
                            {
                                Code = TaskCode,
                                Data = TaskData((long)args[0]!, ++TaskReads),
                            }
                        ),
                        "GetLiveRoomInfo" => Task.FromResult(
                            new BiliApiResponse<GetLiveRoomInfoResponse>
                            {
                                Code = 0,
                                Data = new()
                                {
                                    Live_Status = LiveStatus,
                                    Parent_area_id = ParentAreaId,
                                    Area_id = AreaId,
                                },
                            }
                        ),
                        "LikeLiveRoom" => Like((string)args[0]!),
                        "SendLiveDanmuku" => Send(),
                        _ => throw new InvalidOperationException(method),
                    },
                method =>
                {
                    if (method == "GetFansMedalPanel")
                        Pages++;
                    if (method == "GetLiveRoomInfo")
                        RoomReads++;
                    if (method == "GetActivatedMedalInfo")
                        Clock.Now = Clock.Now.Add(TaskReadDelay);
                }
            );
            Trace = Proxy.Create<ILiveTraceApi>(
                (method, args) =>
                {
                    if (method == "EnterRoom")
                    {
                        Enters++;
                        EntryDevice = ((EnterRoomRequest)args[0]!).Device;
                    }
                    else if (method == "HeartBeat")
                        Heartbeats.Add((HeartBeatRequest)args[0]!);
                    else
                        throw new InvalidOperationException(method);
                    var code =
                        method == "HeartBeat"
                            ? HeartbeatCodeAt?.Invoke(Heartbeats.Count) ?? HeartbeatCode
                            : 0;
                    var serverTimestamp =
                        method == "EnterRoom"
                            ? Clock.Now.ToUnixTimeSeconds()
                            : Heartbeats[^1].Ets + Heartbeats[^1].Time;
                    if (method == "HeartBeat")
                        Clock.Now = Clock.Now.Add(HeartbeatResponseDelay);
                    return Task.FromResult(
                        new BiliApiResponse<HeartBeatResponse>
                        {
                            Code = code,
                            Data = new()
                            {
                                Heartbeat_interval = 30,
                                Secret_key = "synthetic",
                                Timestamp = serverTimestamp,
                            },
                        }
                    );
                }
            );
            Runner = new(
                Api,
                Trace,
                NullLogger.Instance,
                Options,
                "offline-test",
                DelayAsync,
                clock: Clock
            );
        }

        public Task DelayAsync(TimeSpan duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Delays.Add(duration);
            Clock.Now = Clock.Now.Add(duration);
            BeforeDelay?.Invoke();
            return Task.CompletedTask;
        }

        private Task<BiliApiResponse> Like(string request)
        {
            var code = InteractionCodeAt?.Invoke(++InteractionAttempts) ?? 0;
            if (code != 0)
                return Task.FromResult(
                    new BiliApiResponse { Code = code, Message = "synthetic rejection" }
                );
            Likes.Add(
                int.Parse(
                    request.Split('&').First(pair => pair.StartsWith("click_time=")).Split('=')[1]
                )
            );
            return Task.FromResult(new BiliApiResponse { Code = 0 });
        }

        private Task<BiliApiResponse> Send()
        {
            var code = InteractionCodeAt?.Invoke(++InteractionAttempts) ?? 0;
            if (code != 0)
                return Task.FromResult(
                    new BiliApiResponse { Code = code, Message = "synthetic rejection" }
                );
            Danmaku++;
            return Task.FromResult(new BiliApiResponse { Code = 0 });
        }
    }

    public class Proxy : DispatchProxy
    {
        public Func<string, object?[], object> Handler { get; set; } = null!;
        public Action<string>? Before { get; set; }

        protected override object Invoke(MethodInfo? method, object?[]? args)
        {
            Before?.Invoke(method!.Name);
            return Handler(method!.Name, args!);
        }

        public static T Create<T>(
            Func<string, object?[], object> handler,
            Action<string>? before = null
        )
            where T : class
        {
            var value = Create<T, Proxy>();
            var proxy = (Proxy)(object)value;
            proxy.Handler = handler;
            proxy.Before = before;
            return value;
        }
    }
}
