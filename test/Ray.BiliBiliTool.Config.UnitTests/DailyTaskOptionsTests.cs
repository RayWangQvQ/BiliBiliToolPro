#nullable enable

using Ray.BiliBiliTool.Config.Options;
using Xunit;

namespace Ray.BiliBiliTool.Config.UnitTests;

public class DailyTaskOptionsTests
{
    [Fact]
    public void NumberOfCoins_Unconfigured_DefaultsToFive()
    {
        Assert.Equal(5, new DailyTaskOptions().NumberOfCoins);
    }

    [Theory]
    [InlineData(null, new long[0])]
    [InlineData("-1", new long[0])]
    [InlineData(" 1, 2 ", new long[] { 1, 2 })]
    [InlineData("1,invalid", new long[] { 1, long.MinValue })]
    public void SupportUpIdList_ConfiguredValue_ParsesIds(string? value, long[] expected)
    {
        Assert.Equal(expected, new DailyTaskOptions { SupportUpIds = value }.SupportUpIdList);
    }
}
