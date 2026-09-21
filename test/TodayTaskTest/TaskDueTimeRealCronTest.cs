using Ray.BiliBiliTool.Domain;

namespace TodayTaskTest;

/// <summary>
/// 用各任务真实的 Cron 配置验证「今天该不该做」的判定。
/// cron 取自 appsettings.json / Quartz 触发器，格式为「秒 分 时 日 月 周」。
/// </summary>
public class TaskDueTimeRealCronTest
{
    private static readonly TimeSpan CnOffset = TimeSpan.FromHours(8);

    private static DateTimeOffset Cn(int y, int m, int d, int hh, int mm) =>
        new(y, m, d, hh, mm, 0, CnOffset);

    // 2026-09-18 是周五，不是任何任务的特殊日子
    private static readonly DateTimeOffset Now = Cn(2026, 9, 18, 1, 52);

    [Theory]
    // 每天执行的任务：今天都有触发点
    [InlineData("0 0 15 * * ?", true)] // DailyJob        每天 15:00
    [InlineData("0 0 14 * * ?", true)] // MangaJob        每天 14:00
    [InlineData("0 0 15 * * ?", true)] // MangaPrivilege  每天 15:00
    [InlineData("0 0 1 * * ?", true)] // VipPrivilege    每天 01:00
    [InlineData("0 0 8 * * ?", true)] // Silver2Coin     每天 08:00
    [InlineData("0 0 22 * * ?", true)] // LiveLottery     每天 22:00
    [InlineData("0 5 0 * * ?", true)] // LiveFansMedal   每天 00:05
    [InlineData("0 7 1 * * ?", true)] // VipBigPoint     每天 01:07（注意第 3 位是「时」，不是「日」）
    // 按月/按年执行的任务：今天没有触发点
    [InlineData("0 0 12 28 * ?", false)] // ChargeJob   每月 28 号 12:00
    [InlineData("0 0 6 1 * ?", false)] // UnfollowBatched 每月 1 号 06:00
    [InlineData("0 0 0 1 1 ?", false)] // LoginJob/TestBili 每年 1 月 1 日
    public void 按真实cron判断今天有没有触发点(string cron, bool expectedHasFireTimeToday)
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay(cron, Now);

        Assert.Equal(expectedHasFireTimeToday, times.Count > 0);
    }

    [Theory]
    [InlineData("0 0 15 * * ?", false)] // 今天 15:00 还没到
    [InlineData("0 0 1 * * ?", true)] // 今天 01:00 已过
    [InlineData("0 5 0 * * ?", true)] // 今天 00:05 已过
    [InlineData("0 7 1 * * ?", true)] // 今天 01:07 已过
    [InlineData("0 0 22 * * ?", false)] // 今天 22:00 还没到
    public void 按真实cron判断今天是否已到点(string cron, bool expectedDue)
    {
        Assert.Equal(expectedDue, TaskDueTimeCalculator.IsDue(cron, Now));
    }

    [Fact]
    public void 大会员积分cron的第三位是小时_今天01点07分触发()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 7 1 * * ?", Now);

        Assert.Single(times);
        Assert.Equal(Cn(2026, 9, 18, 1, 7), times[0]);
    }

    [Fact]
    public void 每月一号的任务在十八号应当显示本日无需执行()
    {
        var task = TaskCatalog.All.Single(t => t.JobName == "UnfollowBatchedJob");
        var cron = "0 0 6 1 * ?";

        Assert.Empty(TaskDueTimeCalculator.GetFireTimesOfDay(cron, Now));

        Assert.Equal(TodayTaskItemState.NotToday, Evaluate(task, cron, Now));
    }

    [Fact]
    public void 每月一号的任务在当月一号凌晨应当显示等待执行()
    {
        var task = TaskCatalog.All.Single(t => t.JobName == "UnfollowBatchedJob");
        var cron = "0 0 6 1 * ?";
        var firstOfMonth = Cn(2026, 9, 1, 3, 0);

        Assert.Equal(
            Cn(2026, 9, 1, 6, 0),
            TaskDueTimeCalculator.GetFireTimesOfDay(cron, firstOfMonth)[0]
        );

        Assert.Equal(TodayTaskItemState.Waiting, Evaluate(task, cron, firstOfMonth));
    }

    [Fact]
    public void 每天凌晨执行的任务_过了点没跑就显示今天还没做()
    {
        var task = TaskCatalog.All.Single(t => t.JobName == "VipBigPointJob");
        var cron = "0 7 1 * * ?";

        // 当前 01:52，01:07 已过且没有执行记录
        Assert.Equal(TodayTaskItemState.NotDone, Evaluate(task, cron, Now));
    }

    private static TodayTaskItemState Evaluate(TaskDefinition task, string cron, DateTimeOffset now)
    {
        var ctx = new TodayTaskItemContext
        {
            Task = task,
            Item = task.Items[0],
            IsTaskEnabled = true,
            IsItemEnabled = true,
            HasFireTimeToday = TaskDueTimeCalculator.GetFireTimesOfDay(cron, now).Count > 0,
            IsPastDueTime = TaskDueTimeCalculator.IsDue(cron, now),
            Records = [],
            AutoAttempts = 0,
            MaxAutoAttempts = 3,
        };

        return TaskStatusEvaluator.Evaluate(ctx).State;
    }
}
