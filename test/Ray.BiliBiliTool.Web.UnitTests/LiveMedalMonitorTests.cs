using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class LiveMedalMonitorTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 6, 10, 0, 0, TimeSpan.FromHours(8));

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Source : ILiveMedalMonitorSource
    {
        public IReadOnlyList<LiveMedalMonitorAccount> Accounts = [];
        public ConcurrentQueue<LiveMedalMonitorTarget> Started = new();
        public ConcurrentQueue<LiveMedalMonitorTarget> Canceled = new();
        public Func<LiveMedalMonitorTarget, CancellationToken, Task> Run = (_, _) =>
            Task.CompletedTask;
        public int Reads;
        public Action? OnRead;

        public Task<IReadOnlyList<LiveMedalMonitorAccount>> ReadAsync(
            LiveFansMedalTaskOptions options,
            CancellationToken token
        )
        {
            Reads++;
            OnRead?.Invoke();
            return Task.FromResult(Accounts);
        }

        public async Task ExecuteAsync(LiveMedalMonitorTarget target, CancellationToken token)
        {
            Started.Enqueue(target);
            try
            {
                await Run(target, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Canceled.Enqueue(target);
                throw;
            }
        }
    }

    private sealed class Records : ITaskRecordWriter
    {
        public ConcurrentQueue<(
            long User,
            TaskRecordStatus Status,
            TaskRecordTrigger Trigger
        )> Items = new();

        public Task WriteAsync(
            long userId,
            string taskKey,
            string? taskItemKey,
            TaskRecordStatus status,
            string? message,
            TaskRecordTrigger trigger,
            CancellationToken cancellationToken = default
        )
        {
            Items.Enqueue((userId, status, trigger));
            return Task.CompletedTask;
        }
    }

    private sealed class Batches : ITaskFailureBatchMonitor
    {
        public int Begun;
        public int Ended;

        public IDisposable BeginBatch()
        {
            Interlocked.Increment(ref Begun);
            return new Lease(this);
        }

        public Task RecordFailureAsync(
            long? userId,
            string taskKey,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task FlushReadyAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private sealed class Lease(Batches owner) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    Interlocked.Increment(ref owner.Ended);
            }
        }
    }

    private sealed class Environment : IAsyncDisposable
    {
        public readonly Clock Clock = new();
        public readonly Source Source = new();
        public readonly Records Records = new();
        public readonly Batches Batches = new();
        public readonly LiveFansMedalTaskOptions Options = new();
        public readonly LiveMedalMonitorCycle Cycle;

        public Environment() =>
            Cycle = new(
                Source,
                Records,
                Batches,
                Clock,
                NullLogger<LiveMedalMonitorCycle>.Instance
            );

        public LiveMedalMonitorAccount Account(long uid, params LiveMedalCard[] cards) =>
            new((int)uid - 1, uid, "synthetic-v1", new(cards, Clock.Now));

        public Task Tick() => Cycle.TickAsync(Options, CancellationToken.None);

        public ValueTask DisposeAsync() => Cycle.DisposeAsync();
    }

    private static LiveMedalCard Card(
        long id,
        bool live = true,
        string action = "like",
        bool done = false
    ) =>
        new(
            id,
            "示例主播",
            "示例牌",
            30,
            live,
            true,
            false,
            [
                new(
                    action,
                    action == "like" ? "点赞30次"
                        : action == "watchLive" ? "观看15分钟"
                        : "弹幕1次",
                    "每日上限 0/10",
                    done,
                    0
                ),
            ],
            null,
            RoomId: id + 1000
        );

    private static Task WaitUntil(Func<bool> ready) =>
        Task.Run(async () =>
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!ready())
                await Task.Delay(5, deadline.Token);
        });

    [Fact]
    public async Task SameAccountWatchesRoomsInSequenceWhileOtherWorkCanRun()
    {
        await using var env = new Environment();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        env.Source.Run = (target, token) =>
            target.Action == "watchLive" && target.AnchorId == 1
                ? release.Task.WaitAsync(token)
                : Task.CompletedTask;
        env.Source.Accounts =
        [
            env.Account(1, Card(1, action: "watchLive"), Card(2, action: "watchLive"), Card(3)),
            env.Account(2, Card(4, action: "watchLive")),
        ];
        await env.Tick();
        await env.Tick();
        Assert.Equal(
            1,
            Assert
                .Single(
                    env.Source.Started.Where(target =>
                        target.UserId == 1 && target.Action == "watchLive"
                    )
                )
                .AnchorId
        );
        Assert.Contains(
            env.Source.Started,
            target => target.UserId == 1 && target.Action == "like"
        );
        Assert.Contains(
            env.Source.Started,
            target => target.UserId == 2 && target.Action == "watchLive"
        );
        Assert.DoesNotContain(env.Source.Started, target => target.AnchorId == 2);
        release.SetResult();
        await WaitUntil(() => env.Cycle.ActiveCount == 0);
        Assert.Equal(
            new long[] { 1, 2 },
            env.Source.Started.Where(target => target.UserId == 1 && target.Action == "watchLive")
                .Select(target => target.AnchorId)
        );
    }

    [Fact]
    public async Task OutsideWindowWaitsForWatchingButKeepsLikesActive()
    {
        await using var env = new Environment();
        env.Options.UseWatchTimeWindow = true;
        env.Options.WatchStartTime = "11:00";
        env.Source.Accounts = [env.Account(1, Card(1, action: "watchLive"), Card(2))];
        await env.Tick();
        Assert.Equal("like", Assert.Single(env.Source.Started).Action);
        env.Clock.Now = env.Clock.Now.AddHours(1);
        env.Source.Accounts = [env.Account(1, Card(1, action: "watchLive"), Card(2, done: true))];
        await env.Tick();
        Assert.Contains(env.Source.Started, target => target.Action == "watchLive");
    }

    [Fact]
    public async Task QueuedRoomDoesNotStartAfterWindowCloses()
    {
        await using var env = new Environment();
        env.Options.UseWatchTimeWindow = true;
        env.Options.WatchEndTime = "11:00";
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        env.Source.Run = (target, token) => release.Task.WaitAsync(token);
        env.Source.Accounts =
        [
            env.Account(1, Card(1, action: "watchLive"), Card(2, action: "watchLive")),
        ];
        await env.Tick();
        Assert.Single(env.Source.Started);
        env.Clock.Now = env.Clock.Now.AddHours(1);
        release.SetResult();
        await WaitUntil(() => env.Cycle.ActiveCount == 0);
        Assert.Single(env.Source.Started);
        Assert.Single(env.Records.Items);
    }

    [Fact]
    public async Task ChangingWatchWindowOnlyCancelsWatching()
    {
        await using var env = new Environment();
        env.Source.Run = (_, token) => Task.Delay(Timeout.Infinite, token);
        env.Source.Accounts = [env.Account(1, Card(1, action: "watchLive"), Card(2))];
        await env.Tick();
        env.Options.UseWatchTimeWindow = true;
        env.Options.WatchStartTime = "11:00";
        await env.Tick();
        Assert.Equal("watchLive", Assert.Single(env.Source.Canceled).Action);
        Assert.Equal(1, env.Cycle.ActiveCount);
    }

    [Fact]
    public void DefaultsMonitorLiveStateAndUseDailyQuota()
    {
        var options = new LiveFansMedalTaskOptions();
        Assert.True(options.UseLiveStateMonitoring);
        Assert.True(options.FollowDailyTaskLimit);
        Assert.Equal(5, options.MonitorIntervalMinutes);
        Assert.Equal(300, options.DailyLikeNumber);
        Assert.Equal(10, options.DailyDanmakuNumber);
        Assert.Equal(150, options.DailyWatchMinutes);
        Assert.Equal(
            "true",
            options.ToConfigDictionary()["LiveFansMedalTaskConfig:UseLiveStateMonitoring"]
        );
    }

    [Fact]
    public async Task ConfiguredZeroAndReachedDailyCapsDoNotStartActionsOrNotificationBatches()
    {
        await using var env = new Environment();
        env.Options.DailyLikeNumber = 30;
        env.Options.DailyDanmakuNumber = 0;
        env.Options.DailyWatchMinutes = 0;
        var liked = Card(1) with { Tasks = [new("like", "点赞30次", "每日上限 1/10", false, 10)] };
        env.Source.Accounts =
        [
            env.Account(1, liked, Card(2, action: "sendDanmu"), Card(3, action: "watchLive")),
        ];
        await env.Tick();
        Assert.Empty(env.Source.Started);
        Assert.Equal(0, env.Batches.Begun);
        env.Options.DailyLikeNumber = 60;
        await env.Tick();
        Assert.Single(env.Source.Started);
    }

    [Fact]
    public async Task ReadDurationDoesNotMakeFreshSnapshotsAppearToComeFromFuture()
    {
        await using var env = new Environment();
        env.Source.OnRead = () =>
        {
            env.Clock.Now = env.Clock.Now.AddMinutes(2);
            env.Source.Accounts = [env.Account(1, Card(1))];
        };
        await env.Tick();
        Assert.Single(env.Source.Started);
    }

    [Fact]
    public async Task OpeningAndClosingRoomsSelectAppropriateActions()
    {
        await using var env = new Environment();
        env.Options.DanmakuOnlyWhenOffline = true;
        env.Source.Accounts =
        [
            env.Account(1, Card(1, false), Card(2, false, "watchLive"), Card(3, true, "sendDanmu")),
        ];
        await env.Tick();
        await WaitUntil(() => env.Cycle.ActiveCount == 0);
        Assert.Equal("watchLive", Assert.Single(env.Source.Started).Action);
        env.Source.Accounts =
        [
            env.Account(
                1,
                Card(1),
                Card(2, true, "watchLive", done: true),
                Card(3, false, "sendDanmu")
            ),
        ];
        await env.Tick();
        await WaitUntil(() => env.Cycle.ActiveCount == 0);
        Assert.Equal(
            new[] { "like", "sendDanmu", "watchLive" },
            env.Source.Started.Select(target => target.Action).Order().ToArray()
        );
        Assert.All(
            env.Records.Items,
            item => Assert.Equal(TaskRecordTrigger.Scheduled, item.Trigger)
        );
    }

    [Fact]
    public async Task OfflineWatchingStartsAndSurvivesLiveStateChangesWithoutDuplicateSessions()
    {
        await using var env = new Environment();
        env.Source.Run = (_, token) => Task.Delay(Timeout.Infinite, token);
        env.Source.Accounts =
        [
            env.Account(
                1,
                Card(1, false, "watchLive"),
                Card(2, false),
                Card(3, false, "watchLive") with
                {
                    Lighted = false,
                }
            ),
        ];
        await env.Tick();
        Assert.Equal(1, Assert.Single(env.Source.Started).AnchorId);
        env.Source.Accounts = [env.Account(1, Card(1, true, "watchLive"))];
        await env.Tick();
        env.Source.Accounts = [env.Account(1, Card(1, false, "watchLive"))];
        await env.Tick();
        Assert.Single(env.Source.Started);
        Assert.Equal(1, env.Cycle.ActiveCount);
        Assert.Empty(env.Source.Canceled);
    }

    [Fact]
    public async Task ContinuousWatchingDoesNotBlockNewAnchorsOrOtherAccountsAndNeverDuplicates()
    {
        await using var env = new Environment();
        env.Source.Run = (target, token) =>
            target.Action == "watchLive" ? Task.Delay(Timeout.Infinite, token) : Task.CompletedTask;
        env.Source.Accounts = [env.Account(1, Card(1, true, "watchLive"))];
        await env.Tick();
        await env.Tick();
        Assert.Single(env.Source.Started);
        env.Source.Accounts =
        [
            env.Account(1, Card(1, true, "watchLive"), Card(2)),
            env.Account(2, Card(3), Card(4, true, "watchLive")),
        ];
        await env.Tick();
        Assert.Equal(4, env.Source.Started.Count);
        Assert.Equal(2, env.Cycle.ActiveCount);
        Assert.Equal(3, env.Source.Reads);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("timed")]
    [InlineData("actions")]
    [InlineData("emptyWhitelist")]
    public async Task DisabledModesDoNotQueryPlatform(string setting)
    {
        await using var env = new Environment();
        if (setting == "disabled")
            env.Options.IsEnable = false;
        if (setting == "timed")
            env.Options.UseLiveStateMonitoring = false;
        if (setting == "actions")
            env.Options.EnableLike = env.Options.EnableWatch = env.Options.EnableDanmaku = false;
        if (setting == "emptyWhitelist")
            env.Options.OnlySelectedAnchors = true;
        await env.Tick();
        Assert.Equal(0, env.Source.Reads);
    }

    [Fact]
    public async Task CompletedExcludedUnselectedFullUnsupportedAndUnknownCardsAreSkipped()
    {
        await using var env = new Environment();
        env.Options.OnlySelectedAnchors = true;
        env.Options.IncludedAnchorIds = "1,2,3,4,5,6,7,8";
        env.Options.ExcludedAnchorIds = "2";
        env.Source.Accounts =
        [
            env.Account(
                1,
                Card(1, done: true),
                Card(2),
                Card(3) with
                {
                    SavingsFull = true,
                },
                Card(4) with
                {
                    Lighted = null,
                },
                Card(5) with
                {
                    Error = "待刷新",
                },
                Card(6) with
                {
                    RoomId = 0,
                },
                Card(7, action: "unknown"),
                Card(8),
                Card(9)
            ),
        ];
        await env.Tick();
        Assert.Equal(8, Assert.Single(env.Source.Started).AnchorId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-6)]
    [InlineData(2)]
    public async Task OldOrFutureSnapshotsDoNotStartActivities(int minutes)
    {
        await using var env = new Environment();
        var account = env.Account(1, Card(1));
        env.Source.Accounts =
        [
            account with
            {
                Snapshot = account.Snapshot with
                {
                    UpdatedAt =
                        minutes == -1
                            ? env.Clock.Now.AddDays(-1)
                            : env.Clock.Now.AddMinutes(minutes),
                },
            },
        ];
        await env.Tick();
        Assert.Empty(env.Source.Started);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("exclude")]
    [InlineData("credentials")]
    [InlineData("removed")]
    [InlineData("day")]
    public async Task ChangedExecutionSettingsCredentialsOrDayCancelExistingWorkWithoutFailure(
        string change
    )
    {
        await using var env = new Environment();
        env.Source.Run = (_, token) => Task.Delay(Timeout.Infinite, token);
        var account = env.Account(1, Card(1, true, "watchLive"));
        env.Source.Accounts = [account];
        await env.Tick();
        if (change == "disabled")
            env.Options.IsEnable = false;
        if (change == "exclude")
            env.Options.ExcludedAnchorIds = "1";
        if (change == "credentials")
            env.Source.Accounts =
            [
                account with
                {
                    CredentialVersion = "synthetic-v2",
                    Snapshot = account.Snapshot with { Medals = [] },
                },
            ];
        if (change == "removed")
            env.Source.Accounts = [];
        if (change == "day")
            env.Clock.Now = env.Clock.Now.AddDays(1);
        await env.Tick();
        await WaitUntil(() => env.Cycle.ActiveCount == 0);
        Assert.Single(env.Source.Canceled);
        Assert.Empty(env.Records.Items);
    }

    [Fact]
    public async Task DisplayAndIntervalChangesPreserveContinuousWatch()
    {
        await using var env = new Environment();
        env.Source.Run = (_, token) => Task.Delay(Timeout.Infinite, token);
        env.Source.Accounts = [env.Account(1, Card(1, true, "watchLive"))];
        await env.Tick();
        env.Options.PinnedAnchorIds = "1";
        env.Options.MonitorIntervalMinutes = 1;
        env.Options.Cron = "0 0 10 * * ?";
        await env.Tick();
        Assert.Single(env.Source.Started);
        Assert.Empty(env.Source.Canceled);
    }

    [Fact]
    public async Task FailuresAreOneBatchAndWaitFifteenMinutesBeforeRetry()
    {
        await using var env = new Environment();
        env.Source.Run = (_, _) =>
            Task.FromException(new InvalidOperationException("synthetic failure"));
        env.Source.Accounts = [env.Account(1, Card(1), Card(2))];
        await env.Tick();
        await WaitUntil(() => env.Batches.Ended == 1);
        Assert.Equal(2, env.Records.Items.Count(item => item.Status == TaskRecordStatus.Failed));
        Assert.Equal(1, env.Batches.Begun);
        await env.Tick();
        Assert.Equal(2, env.Source.Started.Count);
        Assert.Equal(1, env.Batches.Begun);
        env.Clock.Now = env.Clock.Now.AddMinutes(15);
        env.Source.Accounts = [env.Account(1, Card(1), Card(2))];
        await env.Tick();
        Assert.Equal(4, env.Source.Started.Count);
    }

    [Fact]
    public async Task ExpiredCookieDoesNotCreateASecondTaskFailureReminder()
    {
        await using var env = new Environment();
        env.Source.Run = (_, _) =>
            Task.FromException(
                new InvalidOperationException(
                    "Cookie 已过期，本次活动已跳过，请在账号管理中重新登录"
                )
            );
        env.Source.Accounts = [env.Account(1, Card(1))];
        await env.Tick();
        Assert.Empty(env.Records.Items);
    }

    [Theory]
    [InlineData("like", "点赞30次", "每日上限 7/10", "还需 90 次点赞")]
    [InlineData("sendDanmu", "发送弹幕1次", "每日上限 3/8", "还需 5 条弹幕")]
    [InlineData("watchLive", "观看直播15分钟", "每日上限 7.5/10", "还需 37.5 分钟")]
    public void RemainingQuotaMatchesEachMedalsServerRequirements(
        string action,
        string title,
        string progress,
        string expected
    ) =>
        Assert.Equal(
            expected,
            new LiveMedalTaskProgress(action, title, progress, false, null).RemainingText(
                true,
                false
            )
        );

    [Fact]
    public void MonitoringDoesNotUseLegacyRecoveryAttemptLimit()
    {
        var definition = TaskCatalog.All.Single(task => task.TaskKey == "LiveFansMedalAppService");
        var context = new TodayTaskItemContext
        {
            Task = definition,
            Item = definition.Items[0],
            IsTaskEnabled = true,
            IsItemEnabled = true,
            HasFireTimeToday = true,
            IsPastDueTime = true,
            MonitorMedalLiveState = true,
            FollowMedalDailyTaskLimit = true,
            LiveMedal = new(TodayTaskItemState.NotDone, "待点赞"),
            Records = [],
            AutoAttempts = 3,
            MaxAutoAttempts = 3,
        };
        var result = TaskStatusEvaluator.Evaluate(context);
        Assert.Equal(TodayTaskItemState.NotDone, result.State);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(context, result));
    }
}
