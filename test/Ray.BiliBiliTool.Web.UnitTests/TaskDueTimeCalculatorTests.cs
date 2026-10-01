namespace Ray.BiliBiliTool.Web.UnitTests;

public class TaskDueTimeCalculatorTests
{
    private static readonly TimeSpan CnOffset = TimeSpan.FromHours(8);

    private static DateTimeOffset Cn(int y, int m, int d, int hh, int mm) =>
        new(y, m, d, hh, mm, 0, CnOffset);

    [Fact]
    public void GetFireTimesOfDay_DailyCron_ReturnsSingleFireTime()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 15 * * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Single(times);
        Assert.Equal(Cn(2026, 9, 18, 15, 0), times[0]);
    }

    [Fact]
    public void GetFireTimesOfDay_MonthlyCronBeforeScheduledDate_ReturnsNoFireTimes()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 12 28 * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Empty(times);
    }

    [Fact]
    public void GetFireTimesOfDay_HourlyCron_ReturnsTwentyFourFireTimes()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 * * * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Equal(24, times.Count);
        Assert.Equal(Cn(2026, 9, 18, 0, 0), times[0]);
        Assert.Equal(Cn(2026, 9, 18, 23, 0), times[^1]);
    }

    [Fact]
    public void IsDue_BeforeFireTime_ReturnsFalse()
    {
        Assert.False(TaskDueTimeCalculator.IsDue("0 0 15 * * ?", Cn(2026, 9, 18, 12, 0)));
    }

    [Fact]
    public void IsDue_WithinGracePeriod_ReturnsFalse()
    {
        Assert.False(TaskDueTimeCalculator.IsDue("0 0 15 * * ?", Cn(2026, 9, 18, 15, 5)));
    }

    [Fact]
    public void IsDue_AfterGracePeriod_ReturnsTrue()
    {
        Assert.True(TaskDueTimeCalculator.IsDue("0 0 15 * * ?", Cn(2026, 9, 18, 15, 11)));
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void IsDue_CustomGrace_ChangesThreshold(int minute, bool expected)
    {
        Assert.Equal(
            expected,
            TaskDueTimeCalculator.IsDue(
                "0 0 15 * * ?",
                Cn(2026, 9, 18, 15, minute),
                TimeSpan.FromMinutes(11)
            )
        );
    }

    [Fact]
    public void GetFireTimesOfDay_MidnightTrigger_IsIncludedAtBeginning()
    {
        var times = TaskDueTimeCalculator.GetFireTimesOfDay("0 0 0 * * ?", Cn(2026, 9, 18, 8, 0));

        Assert.Equal(Cn(2026, 9, 18, 0, 0), Assert.Single(times));
    }

    [Fact]
    public void IsDue_NoFireTimeToday_ReturnsFalse()
    {
        Assert.False(TaskDueTimeCalculator.IsDue("0 0 12 28 * ?", Cn(2026, 9, 18, 23, 0)));
    }

    [Fact]
    public void IsDue_InvalidOrNullCron_ReturnsFalseAndNoFireTimes()
    {
        Assert.Empty(TaskDueTimeCalculator.GetFireTimesOfDay("这不是cron", Cn(2026, 9, 18, 8, 0)));
        Assert.False(TaskDueTimeCalculator.IsDue("这不是cron", Cn(2026, 9, 18, 8, 0)));
        Assert.False(TaskDueTimeCalculator.IsDue(null, Cn(2026, 9, 18, 8, 0)));
    }
}
