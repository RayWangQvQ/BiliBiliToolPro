using Ray.BiliBiliTool.Config.Options;
using Xunit;

namespace Ray.BiliBiliTool.Config.UnitTests;

public class CoinDonationPolicyTests
{
    [Theory]
    [InlineData(0, false, 6, false)]
    [InlineData(0, true, 5, false)]
    [InlineData(0, true, 6, true)]
    [InlineData(4, false, 3, false)]
    [InlineData(4, false, 4, true)]
    [InlineData(4, true, 5, true)]
    [InlineData(4, true, null, false)]
    public void CoinPolicy_UsesEachAccountLevelAndPreservesLegacySwitch(
        int stopLevel,
        bool legacy,
        int? accountLevel,
        bool expected
    )
    {
        var options = new DailyTaskOptions
        {
            CoinDonationStopLevel = stopLevel,
            SaveCoinsWhenLv6 = legacy,
        };
        Assert.Equal(expected, options.ShouldSkipCoinDonation(accountLevel));
        Assert.Equal(
            stopLevel.ToString(),
            options.ToConfigDictionary()["DailyTaskConfig:CoinDonationStopLevel"]
        );
    }
}
