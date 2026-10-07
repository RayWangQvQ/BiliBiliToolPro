using Quartz;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class TaskSchedulePlanTests
{
    [Theory]
    [InlineData("0 0 15 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 0 14 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 0 8 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 0 12 28 * ?", SchedulePeriod.Monthly)]
    [InlineData("0 0 6 1 * ?", SchedulePeriod.Monthly)]
    [InlineData("0 0 1 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 7 1 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 0 22 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 5 0 * * ?", SchedulePeriod.Daily)]
    [InlineData("0 0 0 1 1 ?", SchedulePeriod.Yearly)]
    [InlineData("12 30 8 ? * MON-FRI", SchedulePeriod.Weekly)]
    [InlineData("0 30 8 ? * 1,3,7 *", SchedulePeriod.Weekly)]
    [InlineData("0 0 9 L * ?", SchedulePeriod.Monthly)]
    [InlineData("0 0 9 29 2 ?", SchedulePeriod.Yearly)]
    [InlineData("0 15 0/6 * * ?", SchedulePeriod.Hours)]
    [InlineData("7 */15 * * * ?", SchedulePeriod.Minutes)]
    public void ExistingSchedule_RoundTripsWithIdenticalFireTimes(
        string cron,
        SchedulePeriod period
    )
    {
        Assert.True(TaskSchedulePlan.TryParse(cron, out var plan));
        Assert.Equal(period, plan.Period);
        var original = new CronExpression(cron).WithTimeZone(TimeZoneInfo.Utc);
        var generated = new CronExpression(plan.ToCron()).WithTimeZone(TimeZoneInfo.Utc);
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 12; i++)
        {
            var expected = original.GetNextValidTimeAfter(now);
            Assert.Equal(expected, generated.GetNextValidTimeAfter(now));
            Assert.NotNull(expected);
            now = expected!.Value;
        }
    }

    [Theory]
    [InlineData("0 0 8 ? * MON#2")]
    [InlineData("0 0 8 1W * ?")]
    [InlineData("0 0 8 * * ? 2027")]
    [InlineData("0 0/7 * * * ?")]
    [InlineData("invalid")]
    public void SpecialSchedules_AreNotSilentlyConverted(string cron) =>
        Assert.False(TaskSchedulePlan.TryParse(cron, out _));

    [Fact]
    public void LastDay_UsesActualMonthEnd()
    {
        var plan = new TaskSchedulePlan
        {
            Period = SchedulePeriod.Monthly,
            Day = 0,
            Time = new(23, 30, 0),
        };
        var expression = new CronExpression(plan.ToCron()).WithTimeZone(TimeZoneInfo.Utc);
        Assert.Equal(
            new DateTimeOffset(2026, 2, 28, 23, 30, 0, TimeSpan.Zero),
            expression.GetNextValidTimeAfter(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero))
        );
    }

    [Fact]
    public void LocalTime_FiresAtSelectedTimeInServerTimezone()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone(
            "TestUTC8",
            TimeSpan.FromHours(8),
            "UTC+8",
            "UTC+8"
        );
        var expression = new CronExpression(
            new TaskSchedulePlan { Time = new(8, 30, 0) }.ToCron()
        ).WithTimeZone(zone);
        Assert.Equal(
            new DateTimeOffset(2026, 10, 5, 0, 30, 0, TimeSpan.Zero),
            expression.GetNextValidTimeAfter(
                new DateTimeOffset(2026, 10, 5, 0, 0, 0, TimeSpan.Zero)
            )
        );
    }

    [Fact]
    public void InvalidSelections_CannotGenerateSchedules()
    {
        Assert.Throws<FormatException>(() =>
            new TaskSchedulePlan { Period = SchedulePeriod.Weekly, Days = [] }.ToCron()
        );
        Assert.Throws<FormatException>(() =>
            new TaskSchedulePlan
            {
                Period = SchedulePeriod.Yearly,
                Month = 2,
                Day = 31,
            }.ToCron()
        );
        Assert.Throws<FormatException>(() =>
            new TaskSchedulePlan { Period = SchedulePeriod.Hours, Interval = 5 }.ToCron()
        );
        Assert.Throws<FormatException>(() =>
            new TaskSchedulePlan { Time = TimeSpan.FromHours(24) }.ToCron()
        );
    }
}
