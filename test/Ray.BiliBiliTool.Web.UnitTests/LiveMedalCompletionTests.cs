using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class LiveMedalCompletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyTaskListWithoutConfirmedDailyLimitRemainsUnknown(bool lighted)
    {
        var result = Evaluate(new(), Medal(lit: lighted, tasks: []));
        Assert.Equal(TodayTaskItemState.Unknown, result.State);
        Assert.Contains("进度待刷新", result.Message);
        Assert.False(
            TaskStatusEvaluator.CanAutoRedo(
                Context(result),
                TaskStatusEvaluator.Evaluate(Context(result))
            )
        );
    }

    [Fact]
    public void ExplicitDailyLimitCanCompleteAnEmptyTaskList()
    {
        Assert.Equal(
            TodayTaskItemState.Completed,
            Evaluate(new(), Medal(lit: true, tasks: [], full: true)).State
        );
        Assert.Equal(
            TodayTaskItemState.NoWork,
            Evaluate(new() { ExcludedAnchorIds = "1" }, Medal(lit: true, tasks: [])).State
        );
    }

    [Fact]
    public void OutsideWatchWindowCanWaitAndStillAllowManualRecovery()
    {
        var options = new LiveFansMedalTaskOptions
        {
            EnableLike = false,
            EnableDanmaku = false,
            UseWatchTimeWindow = true,
            WatchStartTime = "20:00",
        };
        var card = Medal(
            lit: true,
            tasks: [new("watchLive", "观看15分钟", "每日上限 0/10", false, 0)]
        );
        var progress = Evaluate(options, card);
        Assert.Equal(TodayTaskItemState.WaitingWatchTime, progress.State);
        Assert.Contains("20:00", progress.Message);
        Assert.True(
            new TodayTaskItemDto
            {
                State = progress.State,
                StateText = "等待观看时段",
                DisplayName = "示例",
            }.CanRedo
        );
        Assert.False(
            TaskStatusEvaluator.CanAutoRedo(
                Context(progress),
                TaskStatusEvaluator.Evaluate(Context(progress))
            )
        );
        using var manual = new Ray.BiliBiliTool.Domain.LiveFansMedalWatchScope(true);
        Assert.Equal(TodayTaskItemState.NotDone, Evaluate(options, card).State);
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 5, 18, 42, 0, TimeSpan.FromHours(8));

    private static LiveMedalCard Medal(
        long id = 1,
        bool lit = false,
        bool live = false,
        IReadOnlyList<LiveMedalTaskProgress>? tasks = null,
        string? error = null,
        bool full = false
    ) =>
        new(
            id,
            "示例主播",
            "示例牌",
            12,
            live,
            lit,
            full,
            tasks
                ??
                [
                    new("like", "点赞10次", "仅点亮", false, null),
                    new("sendDanmu", "发送1条弹幕", "仅点亮", false, null),
                ],
            error
        );

    private static LiveMedalCompletion Evaluate(
        LiveFansMedalTaskOptions options,
        params LiveMedalCard[] medals
    ) => LiveMedalCompletionEvaluator.Evaluate(new(medals, Now), options, Now);

    private static TodayTaskItemContext Context(
        LiveMedalCompletion? completion,
        bool follow = false,
        TaskRecordStatus status = TaskRecordStatus.Success,
        int attempts = 1
    ) =>
        new()
        {
            Task = TaskCatalog.All.Single(task => task.TaskKey == "LiveFansMedalAppService"),
            Item = TaskCatalog.All.Single(task => task.TaskKey == "LiveFansMedalAppService").Items[
                0
            ],
            IsTaskEnabled = true,
            IsItemEnabled = true,
            HasFireTimeToday = true,
            IsPastDueTime = true,
            LiveMedal = completion,
            FollowMedalDailyTaskLimit = follow,
            Records =
            [
                new()
                {
                    UserId = 1001,
                    TaskKey = "LiveFansMedalAppService",
                    RecordDate = "2026-10-05",
                    Status = status,
                    Trigger = TaskRecordTrigger.Auto,
                    CreatedAtUtc = Now.AddHours(-14),
                },
            ],
            AutoAttempts = attempts,
            MaxAutoAttempts = 3,
        };

    [Fact]
    public void OldSuccessfulExecutionCannotCompleteFiveUnlitMedals()
    {
        var progress = Evaluate(new(), Enumerable.Range(1, 5).Select(id => Medal(id)).ToArray());
        var ctx = Context(progress);
        var result = TaskStatusEvaluator.Evaluate(ctx);
        Assert.Equal(TodayTaskItemState.NotDone, result.State);
        Assert.Equal("已完成 0 / 5 个粉丝牌 · 待点亮 5 个", result.Message);
        Assert.Null(result.CompletedAt);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void LightingAlternativesDoNotRequireBothActionsAfterMedalBecomesLit()
    {
        var result = Evaluate(new(), Medal(lit: true));
        Assert.Equal(TodayTaskItemState.Completed, result.State);
        Assert.Contains("1 / 1", result.Message);
    }

    [Fact]
    public void DoneFlagsWithoutConfirmedLightingDoNotCompleteMedal()
    {
        var result = Evaluate(new(), Medal(tasks: [new("like", "点赞", "仅点亮", true, 100)]));
        Assert.Equal(TodayTaskItemState.WaitingConditions, result.State);
    }

    [Fact]
    public void ExcludedAndUnselectedAnchorsDoNotAffectCompletion()
    {
        var result = Evaluate(
            new()
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = false,
                OnlySelectedAnchors = true,
                IncludedAnchorIds = "1,2",
                ExcludedAnchorIds = "2",
            },
            Medal(1, lit: true),
            Medal(2, error: "暂未获取"),
            Medal(3)
        );
        Assert.Equal(TodayTaskItemState.Completed, result.State);
        Assert.Contains("1 / 1", result.Message);
    }

    [Fact]
    public void OnlyEnabledPositiveBudgetActionsAreChecked()
    {
        var result = Evaluate(
            new()
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = false,
                EnableDanmaku = false,
                LikeNumber = 0,
            },
            Medal(
                lit: true,
                tasks:
                [
                    new("like", "点赞", "0/6", false, 0),
                    new("sendDanmu", "弹幕", "0/10", false, 0),
                    new("watchLive", "观看", "7/7", true, 100),
                ]
            )
        );
        Assert.Equal(TodayTaskItemState.Completed, result.State);
    }

    [Fact]
    public void FollowModeChecksEnabledActionsEvenWithZeroCustomBudgets()
    {
        var result = Evaluate(
            new()
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = true,
                LikeNumber = 0,
            },
            Medal(lit: true, live: true, tasks: [new("like", "点赞", "0/6", false, 0)])
        );
        Assert.Equal(TodayTaskItemState.NotDone, result.State);
    }

    [Fact]
    public void OfflineLikesWaitWithoutConsumingRecoveryAttempts()
    {
        var progress = Evaluate(
            new()
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = false,
                EnableDanmaku = false,
                EnableWatch = false,
            },
            Medal(
                lit: true,
                tasks:
                [
                    new("like", "点赞", "0/6", false, 0),
                    new("watchLive", "观看", "0/7", false, 0),
                ]
            )
        );
        var ctx = Context(progress, follow: true, attempts: 3);
        var result = TaskStatusEvaluator.Evaluate(ctx);
        Assert.Equal(TodayTaskItemState.WaitingConditions, result.State);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void OfflineWatchingIsActionableAndOnlyPlatformProgressCompletesIt()
    {
        var options = new LiveFansMedalTaskOptions { EnableLike = false, EnableDanmaku = false };
        var task = new LiveMedalTaskProgress("watchLive", "观看15分钟", "每日上限 0/10", false, 0);
        Assert.Equal(
            TodayTaskItemState.NotDone,
            Evaluate(options, Medal(lit: true, tasks: [task])).State
        );
        Assert.Equal(
            TodayTaskItemState.Completed,
            Evaluate(
                options,
                Medal(lit: true, tasks: [task with { Done = true, Percent = 100 }])
            ).State
        );
    }

    [Fact]
    public void OfflineOnlyMessagesWaitWhileAnchorIsLive()
    {
        var result = Evaluate(
            new()
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = false,
                DanmakuOnlyWhenOffline = true,
                EnableLike = false,
            },
            Medal(live: true)
        );
        Assert.Equal(TodayTaskItemState.WaitingConditions, result.State);
    }

    [Fact]
    public void FullSavingsSkipUpgradeButCannotSubstituteForLighting()
    {
        Assert.Equal(
            TodayTaskItemState.Completed,
            Evaluate(new(), Medal(lit: true, full: true)).State
        );
        Assert.Equal(TodayTaskItemState.NotDone, Evaluate(new(), Medal(full: true)).State);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoSelectedAnchorsOrNoEnabledActionsAreNotCountedAsCompleted(bool emptySelection)
    {
        var options = emptySelection
            ? new LiveFansMedalTaskOptions
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = false,
                OnlySelectedAnchors = true,
            }
            : new LiveFansMedalTaskOptions
            {
                UseLiveStateMonitoring = false,
                FollowDailyTaskLimit = false,
                LikeNumber = 0,
                SendDanmakuNumber = 0,
                HeartBeatNumber = 0,
            };
        Assert.Equal(TodayTaskItemState.NoWork, Evaluate(options, Medal()).State);
    }

    [Theory]
    [InlineData(-1440)]
    [InlineData(-6)]
    [InlineData(2)]
    public void StaleOrFutureSnapshotsCannotCompleteToday(int minutes)
    {
        var result = LiveMedalCompletionEvaluator.Evaluate(
            new([Medal(lit: true)], Now.AddMinutes(minutes)),
            new(),
            Now
        );
        Assert.Equal(TodayTaskItemState.Unknown, result.State);
    }

    [Fact]
    public void ParticipatingProgressErrorsOverrideOldSuccessAndDoNotTriggerRecovery()
    {
        var ctx = Context(Evaluate(new(), Medal(error: "暂未获取")), follow: true);
        var result = TaskStatusEvaluator.Evaluate(ctx);
        Assert.Equal(TodayTaskItemState.Unknown, result.State);
        Assert.Null(result.CompletedAt);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void PendingFirstPhaseNeverCompletesFromExecutionRecords()
    {
        Assert.Equal(TodayTaskItemState.Unknown, TaskStatusEvaluator.Evaluate(Context(null)).State);
    }

    [Fact]
    public void FollowModeCanRecoverActionableRemainingProgressAfterSuccess()
    {
        var ctx = Context(Evaluate(new(), Medal()), follow: true);
        Assert.True(TaskStatusEvaluator.CanAutoRedo(ctx, TaskStatusEvaluator.Evaluate(ctx)));
    }

    [Fact]
    public void CompletedPlatformGoalsTakePriorityOverEarlierFailureAndRetryLimit()
    {
        var ctx = Context(
            Evaluate(new(), Medal(lit: true)),
            status: TaskRecordStatus.Failed,
            attempts: 3
        );
        Assert.Equal(TodayTaskItemState.Completed, TaskStatusEvaluator.Evaluate(ctx).State);
    }

    [Fact]
    public void RoomsWithoutAnInteractionTargetAreNotCounted()
    {
        Assert.Equal(
            TodayTaskItemState.NoWork,
            Evaluate(new(), Medal() with { CanInteract = false }).State
        );
    }
}
