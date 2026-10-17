using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TaskStatusEvaluatorTests
{
    private static readonly TaskDefinition DailyTask = TaskCatalog.All.First(t =>
        t.TaskKey == "DailyTaskAppService"
    );

    private static TaskItemDefinition Item(string key) =>
        DailyTask.Items.Single(i => i.ItemKey == key);

    private static TaskRecord Rec(
        TaskRecordStatus status,
        string? itemKey,
        TaskRecordTrigger trigger = TaskRecordTrigger.Scheduled
    ) =>
        new()
        {
            UserId = 1,
            TaskKey = DailyTask.TaskKey,
            TaskItemKey = itemKey,
            RecordDate = DateTimeOffset.Now.ToString("yyyy-MM-dd"),
            Status = status,
            Trigger = trigger,
        };

    private static TodayTaskItemContext Ctx(
        string itemKey,
        bool isTaskEnabled = true,
        bool isItemEnabled = true,
        bool hasFireTimeToday = true,
        bool isPastDueTime = true,
        BiliDailyRewardSnapshot? bili = null,
        bool biliQueryFailed = false,
        IReadOnlyList<TaskRecord>? records = null,
        int autoAttempts = 0,
        int maxAutoAttempts = 3
    ) =>
        new()
        {
            Task = DailyTask,
            Item = Item(itemKey),
            IsTaskEnabled = isTaskEnabled,
            IsItemEnabled = isItemEnabled,
            HasFireTimeToday = hasFireTimeToday,
            IsPastDueTime = isPastDueTime,
            BiliReward = bili,
            BiliQueryFailed = biliQueryFailed,
            Records = records ?? [],
            AutoAttempts = autoAttempts,
            MaxAutoAttempts = maxAutoAttempts,
        };

    [Fact]
    public void Evaluate_DisabledTask_ReturnsDisabled()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", isTaskEnabled: false));
        Assert.Equal(TodayTaskItemState.Disabled, r.State);
    }

    [Fact]
    public void Evaluate_DisabledItem_ReturnsDisabled()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx("Share", isItemEnabled: false, bili: new(false, true, false, 0))
        );
        Assert.Equal(TodayTaskItemState.Disabled, r.State);
    }

    [Fact]
    public void Evaluate_NoFireTimeToday_ReturnsNotToday()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", hasFireTimeToday: false));
        Assert.Equal(TodayTaskItemState.NotToday, r.State);
    }

    [Fact]
    public void Evaluate_BeforeFireTime_ReturnsWaiting()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx("Login", isPastDueTime: false, bili: new(false, false, false, 0))
        );
        Assert.Equal(TodayTaskItemState.Waiting, r.State);
    }

    [Fact]
    public void Evaluate_BiliQueryFailure_ReturnsUnknown()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", biliQueryFailed: true));
        Assert.Equal(TodayTaskItemState.Unknown, r.State);
    }

    [Fact]
    public void Evaluate_BiliReportsCompletion_ReturnsCompleted()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", bili: new(true, true, false, 50)));
        Assert.Equal(TodayTaskItemState.Completed, r.State);
    }

    [Fact]
    public void Evaluate_TaskLevelSuccessRecord_ReturnsCompleted()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx("VipPrivilege", records: [Rec(TaskRecordStatus.Success, null)])
        );
        Assert.Equal(TodayTaskItemState.Completed, r.State);
    }

    [Fact]
    public void Evaluate_NoRecordsAndIncompleteBiliReward_ReturnsNotDone()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("DonateCoin", bili: new(true, true, false, 0)));
        Assert.Equal(TodayTaskItemState.NotDone, r.State);
    }

    [Fact]
    public void Evaluate_SuccessRecordButIncompleteBiliReward_ReturnsFailed()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx(
                "DonateCoin",
                bili: new(true, true, false, 0),
                records: [Rec(TaskRecordStatus.Success, null)]
            )
        );
        Assert.Equal(TodayTaskItemState.Failed, r.State);
    }

    [Fact]
    public void Evaluate_ShareRejectedByBili_ReturnsFailedWithoutRetryExhaustion()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx(
                "Share",
                autoAttempts: 3,
                bili: new(true, true, false, 50),
                records: [Rec(TaskRecordStatus.Success, null, TaskRecordTrigger.Auto)]
            )
        );
        Assert.Equal(TodayTaskItemState.Failed, r.State);
        Assert.Contains("B站", r.Message);
    }

    [Fact]
    public void Evaluate_AutoRetryLimitReached_ReturnsRetryExhausted()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx(
                "DonateCoin",
                autoAttempts: 3,
                maxAutoAttempts: 3,
                bili: new(true, true, false, 0),
                records: [Rec(TaskRecordStatus.Failed, "DonateCoin", TaskRecordTrigger.Auto)]
            )
        );
        Assert.Equal(TodayTaskItemState.RetryExhausted, r.State);
    }

    [Fact]
    public void Evaluate_TaskLevelFailureBelowRetryLimit_ReturnsFailedMessage()
    {
        var manga = TaskCatalog.All.First(t => t.TaskKey == "MangaTaskAppService");
        var ctx = new TodayTaskItemContext
        {
            Task = manga,
            Item = manga.Items[0],
            IsTaskEnabled = true,
            IsItemEnabled = true,
            HasFireTimeToday = true,
            IsPastDueTime = true,
            Records =
            [
                new TaskRecord
                {
                    UserId = 1,
                    TaskKey = manga.TaskKey,
                    TaskItemKey = null,
                    RecordDate = DateTimeOffset.Now.ToString("yyyy-MM-dd"),
                    Status = TaskRecordStatus.Failed,
                    Message = "配置直播Cookie失败",
                    Trigger = TaskRecordTrigger.Scheduled,
                },
            ],
            AutoAttempts = 1,
            MaxAutoAttempts = 3,
        };

        var r = TaskStatusEvaluator.Evaluate(ctx);
        Assert.Equal(TodayTaskItemState.Failed, r.State);
        Assert.Equal("配置直播Cookie失败", r.Message);
    }

    [Fact]
    public void CanAutoRedo_MissingTask_AllowsRetry()
    {
        var ctx = Ctx("DonateCoin", bili: new(true, true, false, 0));
        var result = TaskStatusEvaluator.Evaluate(ctx);

        Assert.Equal(TodayTaskItemState.NotDone, result.State);
        Assert.True(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void CanAutoRedo_FailedTaskBelowRetryLimit_AllowsRetry()
    {
        var ctx = Ctx(
            "DonateCoin",
            bili: new(true, true, false, 0),
            records: [Rec(TaskRecordStatus.Success, null)]
        );
        var result = TaskStatusEvaluator.Evaluate(ctx);

        Assert.Equal(TodayTaskItemState.Failed, result.State);
        Assert.True(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void CanAutoRedo_FailedShareTask_ReturnsFalse()
    {
        var ctx = Ctx(
            "Share",
            bili: new(true, true, false, 50),
            records: [Rec(TaskRecordStatus.Success, null)]
        );
        var result = TaskStatusEvaluator.Evaluate(ctx);

        Assert.Equal(TodayTaskItemState.Failed, result.State);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void CanAutoRedo_RetryLimitReached_ReturnsFalse()
    {
        var ctx = Ctx(
            "DonateCoin",
            autoAttempts: 3,
            bili: new(true, true, false, 0),
            records: [Rec(TaskRecordStatus.Failed, "DonateCoin", TaskRecordTrigger.Auto)]
        );
        var result = TaskStatusEvaluator.Evaluate(ctx);

        Assert.Equal(TodayTaskItemState.RetryExhausted, result.State);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(ctx, result));

        // 页面上的 StateText 已经写了「已自动重试 N 次仍未完成」，
        // Message 必须为空，否则会渲染成重复文案。
        Assert.Null(result.Message);
    }

    [Fact]
    public void CanAutoRedo_CompletedOrUnknownTask_ReturnsFalse()
    {
        var done = Ctx("Login", bili: new(true, true, false, 50));
        Assert.False(TaskStatusEvaluator.CanAutoRedo(done, TaskStatusEvaluator.Evaluate(done)));

        var unknown = Ctx("Login", biliQueryFailed: true);
        Assert.False(
            TaskStatusEvaluator.CanAutoRedo(unknown, TaskStatusEvaluator.Evaluate(unknown))
        );
    }

    [Fact]
    public void Evaluate_BiliQueryFailsDespiteSuccessfulRecord_RemainsUnknown()
    {
        var result = TaskStatusEvaluator.Evaluate(
            Ctx("Login", biliQueryFailed: true, records: [Rec(TaskRecordStatus.Success, "Login")])
        );

        Assert.Equal(TodayTaskItemState.Unknown, result.State);
        Assert.False(TaskStatusEvaluator.CanAutoRedo(Ctx("Login", biliQueryFailed: true), result));
    }

    [Fact]
    public void Evaluate_OneAttemptBelowLimit_AllowsRedo()
    {
        var ctx = Ctx(
            "DonateCoin",
            autoAttempts: 2,
            maxAutoAttempts: 3,
            records: [Rec(TaskRecordStatus.Failed, "DonateCoin")]
        );
        var result = TaskStatusEvaluator.Evaluate(ctx);

        Assert.Equal(TodayTaskItemState.Failed, result.State);
        Assert.True(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }
}

public class TaskCatalogTests
{
    [Fact]
    public void All_TaskKeysAndJobNames_AreUnique()
    {
        Assert.Equal(
            TaskCatalog.All.Count,
            TaskCatalog.All.Select(t => t.TaskKey).Distinct().Count()
        );
        Assert.Equal(
            TaskCatalog.All.Count,
            TaskCatalog.All.Select(t => t.JobName).Distinct().Count()
        );
    }

    [Fact]
    public void All_DailyTask_ContainsFourBiliRewardItems()
    {
        var daily = TaskCatalog.All.Single(t => t.TaskKey == "DailyTaskAppService");
        var biliItems = daily
            .Items.Where(i => i.Source == TaskItemSource.BiliDailyReward)
            .Select(i => i.ItemKey)
            .ToList();

        Assert.Equal(4, biliItems.Count);
        Assert.Contains("Login", biliItems);
        Assert.Contains("Watch", biliItems);
        Assert.Contains("Share", biliItems);
        Assert.Contains("DonateCoin", biliItems);
    }

    [Fact]
    public void IsEnabled_ZeroCoinTarget_DisablesDonateCoinItem()
    {
        var daily = TaskCatalog.All.Single(t => t.TaskKey == "DailyTaskAppService");
        var coin = daily.Items.Single(i => i.ItemKey == "DonateCoin");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["DailyTaskConfig:NumberOfCoins"] = "0" }
            )
            .Build();

        Assert.False(coin.IsEnabled(config));
    }

    [Fact]
    public void IsEnabled_ShareDisabled_DisablesShareItem()
    {
        var daily = TaskCatalog.All.Single(t => t.TaskKey == "DailyTaskAppService");
        var share = daily.Items.Single(i => i.ItemKey == TaskCatalog.ShareItemKey);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["DailyTaskConfig:IsShareVideo"] = "false" }
            )
            .Build();

        Assert.False(share.IsEnabled(config));
    }
}
