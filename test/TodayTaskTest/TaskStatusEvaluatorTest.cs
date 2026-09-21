using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Domain;

namespace TodayTaskTest;

public class TaskStatusEvaluatorTest
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
    public void 任务被关闭时显示已关闭()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", isTaskEnabled: false));
        Assert.Equal(TodayTaskItemState.Disabled, r.State);
    }

    [Fact]
    public void 单项被关闭时显示已关闭_比如关掉分享()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx("Share", isItemEnabled: false, bili: new(false, true, false, 0))
        );
        Assert.Equal(TodayTaskItemState.Disabled, r.State);
    }

    [Fact]
    public void 今天没有触发点显示本日无需执行()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", hasFireTimeToday: false));
        Assert.Equal(TodayTaskItemState.NotToday, r.State);
    }

    [Fact]
    public void 还没到今天的触发时间显示等待执行()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx("Login", isPastDueTime: false, bili: new(false, false, false, 0))
        );
        Assert.Equal(TodayTaskItemState.Waiting, r.State);
    }

    [Fact]
    public void B站查询失败显示状态未知且不参与补做()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", biliQueryFailed: true));
        Assert.Equal(TodayTaskItemState.Unknown, r.State);
    }

    [Fact]
    public void B站确认完成则为已完成()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("Login", bili: new(true, true, false, 50)));
        Assert.Equal(TodayTaskItemState.Completed, r.State);
    }

    [Fact]
    public void 执行记录里有成功则为已完成_用于任务级检查项()
    {
        var r = TaskStatusEvaluator.Evaluate(
            Ctx("VipPrivilege", records: [Rec(TaskRecordStatus.Success, null)])
        );
        Assert.Equal(TodayTaskItemState.Completed, r.State);
    }

    [Fact]
    public void 完全没有记录且B站未完成则为未执行()
    {
        var r = TaskStatusEvaluator.Evaluate(Ctx("DonateCoin", bili: new(true, true, false, 0)));
        Assert.Equal(TodayTaskItemState.NotDone, r.State);
    }

    [Fact]
    public void 今天跑过但B站仍显示未完成则为失败_投币场景()
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
    public void 分享被B站拒绝时显示失败且不消耗自动重试()
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
    public void 自动重试达上限后不再重试_即使仍然失败()
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
    public void 任务级检查项失败且未达上限时为失败()
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
    public void 漏做的项允许自动补做()
    {
        var ctx = Ctx("DonateCoin", bili: new(true, true, false, 0));
        var result = TaskStatusEvaluator.Evaluate(ctx);

        Assert.Equal(TodayTaskItemState.NotDone, result.State);
        Assert.True(TaskStatusEvaluator.CanAutoRedo(ctx, result));
    }

    [Fact]
    public void 失败但未达上限的项允许自动补做()
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
    public void 分享永远不自动补做_避免每天白试三次()
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
    public void 已达重试上限的项不自动补做()
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
    public void 已完成与状态未知的项都不自动补做()
    {
        var done = Ctx("Login", bili: new(true, true, false, 50));
        Assert.False(TaskStatusEvaluator.CanAutoRedo(done, TaskStatusEvaluator.Evaluate(done)));

        var unknown = Ctx("Login", biliQueryFailed: true);
        Assert.False(
            TaskStatusEvaluator.CanAutoRedo(unknown, TaskStatusEvaluator.Evaluate(unknown))
        );
    }
}

public class TaskCatalogTest
{
    [Fact]
    public void 目录里的任务键与任务名互不重复()
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
    public void 每日任务包含登录观看分享投币四个B站检查项()
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
    public void 投币数配成0时投币项视为关闭()
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
    public void 关掉分享开关后分享项视为关闭()
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
