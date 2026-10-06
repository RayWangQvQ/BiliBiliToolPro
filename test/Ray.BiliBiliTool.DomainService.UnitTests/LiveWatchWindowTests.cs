using System.ComponentModel.DataAnnotations;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Xunit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveWatchWindowTests
{
    [Theory]
    [InlineData("08:00", "23:00", 7, 59, false, 8, 0, 0)]
    [InlineData("08:00", "23:00", 8, 0, true, 23, 0, 0)]
    [InlineData("08:00", "23:00", 22, 59, true, 23, 0, 0)]
    [InlineData("08:00", "23:00", 23, 0, false, 8, 0, 1)]
    [InlineData("22:00", "02:00", 21, 59, false, 22, 0, 0)]
    [InlineData("22:00", "02:00", 22, 0, true, 2, 0, 1)]
    [InlineData("22:00", "02:00", 1, 59, true, 2, 0, 0)]
    [InlineData("22:00", "02:00", 2, 0, false, 22, 0, 0)]
    public void UsesUtc8AndExclusiveEndWithOvernightBoundaries(
        string start,
        string end,
        int hour,
        int minute,
        bool allowed,
        int nextHour,
        int nextMinute,
        int days
    )
    {
        var options = new LiveFansMedalTaskOptions
        {
            UseWatchTimeWindow = true,
            WatchStartTime = start,
            WatchEndTime = end,
        };
        var local = new DateTimeOffset(2026, 10, 7, hour, minute, 0, TimeSpan.FromHours(8));
        Assert.Equal(allowed, options.IsWatchTimeAllowed(local.ToUniversalTime()));
        var next = new DateTimeOffset(
            local.Date.AddDays(days).AddHours(nextHour).AddMinutes(nextMinute),
            local.Offset
        );
        Assert.Equal(next, options.NextWatchWindowBoundary(local));
    }

    [Theory]
    [InlineData("08:00", "08:00")]
    [InlineData("24:00", "23:00")]
    [InlineData("8:00", "23:00")]
    [InlineData("08:00", "23:60")]
    public void InvalidWindowsCannotBeSavedOrStartWatching(string start, string end)
    {
        var options = new LiveFansMedalTaskOptions
        {
            UseWatchTimeWindow = true,
            WatchStartTime = start,
            WatchEndTime = end,
        };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(options, new(options), errors, true));
        Assert.False(options.IsWatchTimeAllowed(DateTimeOffset.UtcNow));
        Assert.Null(options.NextWatchWindowBoundary(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void LegacyConfigurationRemainsAllDayAndNewFieldsRoundTrip()
    {
        var options = new LiveFansMedalTaskOptions();
        Assert.False(options.UseWatchTimeWindow);
        Assert.True(options.IsWatchTimeAllowed(DateTimeOffset.UtcNow));
        Assert.Null(options.NextWatchWindowBoundary(DateTimeOffset.UtcNow));
        options.UseWatchTimeWindow = true;
        options.WatchStartTime = "22:30";
        options.WatchEndTime = "02:15";
        var values = options.ToConfigDictionary();
        Assert.Equal("true", values["LiveFansMedalTaskConfig:UseWatchTimeWindow"]);
        Assert.Equal("22:30", values["LiveFansMedalTaskConfig:WatchStartTime"]);
        Assert.Equal("02:15", values["LiveFansMedalTaskConfig:WatchEndTime"]);
    }

    [Fact]
    public async Task ManualScopeIsNestedIsolatedAndRestored()
    {
        Assert.False(LiveFansMedalWatchScope.IsManual);
        using (var manual = new LiveFansMedalWatchScope(true))
        {
            Assert.True(LiveFansMedalWatchScope.IsManual);
            await Task.Run(() =>
            {
                using var automatic = new LiveFansMedalWatchScope(false);
                Assert.False(LiveFansMedalWatchScope.IsManual);
            });
            Assert.True(LiveFansMedalWatchScope.IsManual);
            manual.Dispose();
            manual.Dispose();
        }
        Assert.False(LiveFansMedalWatchScope.IsManual);
    }
}
