namespace TodayTaskTest;

public class TaskDueTimeCalculatorTest
{
    private static readonly TimeSpan CnOffset = TimeSpan.FromHours(8);

    private static DateTimeOffset Cn(int y, int m, int d, int hh, int mm) =>
        new(y, m, d, hh, mm, 0, CnOffset);

    [Fact]
    public void 每日一次的cron_今天只有一个触发点()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 15 * * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Single(times);
        Assert.Equal(Cn(2026, 9, 18, 15, 0), times[0]);
    }

    [Fact]
    public void 每月28号的cron_在18号没有触发点()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 12 28 * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Empty(times);
    }

    [Fact]
    public void 每小时一次的cron_今天有24个触发点()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 * * * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Equal(24, times.Count);
        Assert.Equal(Cn(2026, 9, 18, 0, 0), times[0]);
        Assert.Equal(Cn(2026, 9, 18, 23, 0), times[^1]);
    }

    [Fact]
    public void 未到今天的触发时间_判定为未到点()
    {
        Assert.False(TaskDueTimeCalculator.IsDue("0 0 15 * * ?", Cn(2026, 9, 18, 12, 0)));
    }

    [Fact]
    public void 刚过触发时间但在宽限期内_仍判定为未到点()
    {
        Assert.False(TaskDueTimeCalculator.IsDue("0 0 15 * * ?", Cn(2026, 9, 18, 15, 5)));
    }

    [Fact]
    public void 超过触发时间加宽限期_判定为已到点()
    {
        Assert.True(TaskDueTimeCalculator.IsDue("0 0 15 * * ?", Cn(2026, 9, 18, 15, 11)));
    }

    [Fact]
    public void 今天没有触发点_永远不算到点()
    {
        Assert.False(TaskDueTimeCalculator.IsDue("0 0 12 28 * ?", Cn(2026, 9, 18, 23, 0)));
    }

    [Fact]
    public void 非法cron_返回空且不算到点()
    {
        Assert.Empty(TaskDueTimeCalculator.GetFireTimesOfDay("这不是cron", Cn(2026, 9, 18, 8, 0)));
        Assert.False(TaskDueTimeCalculator.IsDue("这不是cron", Cn(2026, 9, 18, 8, 0)));
        Assert.False(TaskDueTimeCalculator.IsDue(null, Cn(2026, 9, 18, 8, 0)));
    }
}
