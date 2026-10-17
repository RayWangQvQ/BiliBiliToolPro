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
    public async Task Runner_OfflineAndRoundPlayRoomsDoNotReceiveLikesOrHeartbeats(int status)
    {
        var env = new Environment { LiveStatus = status };
        await env.Runner.RunAsync(env.Cookie, "like");
        await env.Runner.RunAsync(env.Cookie, "watchLive");
        Assert.Empty(env.Likes);
        Assert.Equal(0, env.Enters);
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
            }
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

    private class Environment
    {
        public LiveFansMedalTaskOptions Options { get; } = new();
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
        public List<int> Likes { get; } = [];
        public List<HeartBeatRequest> Heartbeats { get; } = [];
        public List<TimeSpan> Delays { get; } = [];
        public int LiveStatus { get; set; } = 1;
        public int TaskCode { get; set; }
        public int Pages { get; set; }
        public int TaskReads { get; set; }
        public int Enters { get; set; }
        public int Danmaku { get; set; }
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
                                    Parent_area_id = 1,
                                    Area_id = 1,
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
                    return Task.FromResult(
                        new BiliApiResponse<HeartBeatResponse>
                        {
                            Code = 0,
                            Data = new()
                            {
                                Heartbeat_interval = 30,
                                Secret_key = "synthetic",
                                Timestamp = 1,
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
                (duration, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    Delays.Add(duration);
                    return Task.CompletedTask;
                }
            );
        }

        private Task<BiliApiResponse> Like(string request)
        {
            Likes.Add(
                int.Parse(
                    request.Split('&').First(pair => pair.StartsWith("click_time=")).Split('=')[1]
                )
            );
            return Task.FromResult(new BiliApiResponse { Code = 0 });
        }

        private Task<BiliApiResponse> Send()
        {
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
