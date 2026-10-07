using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class UpgradeForecastStopLevelTests
{
    [Theory]
    [InlineData(5, 5, false, 5, true, true, 7)]
    [InlineData(5, 4, false, 5, true, true, 7)]
    [InlineData(1, 1, false, 5, true, true, 7)]
    [InlineData(4, 5, false, 5, true, true, 2)]
    [InlineData(5, 6, true, 5, true, true, 2)]
    [InlineData(5, 0, true, 5, true, true, 2)]
    [InlineData(5, 0, false, 5, true, true, 2)]
    [InlineData(5, 5, true, 5, true, true, 7)]
    [InlineData(5, 5, false, 5, false, false, 20)]
    [InlineData(5, 5, false, 5, false, true, 10)]
    [InlineData(5, 5, false, 5, true, false, 10)]
    [InlineData(5, 0, false, 0, true, true, 7)]
    public void Forecast_UsesTheSameDonationStopRuleAsExecution(
        int level,
        int stopLevel,
        bool legacySaveAtSix,
        int coins,
        bool watch,
        bool share,
        int expectedDays
    )
    {
        var options = new DailyTaskOptions
        {
            CoinDonationStopLevel = stopLevel,
            SaveCoinsWhenLv6 = legacySaveAtSix,
            NumberOfCoins = coins,
            IsWatchVideo = watch,
            IsShareVideo = share,
        };
        var service = new AccountDomainService(
            NullLogger<AccountDomainService>.Instance,
            null!,
            null!,
            new Monitor<UnfollowBatchedTaskOptions>(new()),
            new Monitor<DailyTaskOptions>(options)
        );
        var account = new UserInfo
        {
            Money = 20,
            Level_info = new()
            {
                Current_level = level,
                Current_exp = 0,
                Next_exp = 100,
            },
            Wbi_img = new() { img_url = "", sub_url = "" },
        };

        Assert.Equal(expectedDays, service.CalculateUpgradeTime(account));
    }

    private sealed class Monitor<T>(T options) : IOptionsMonitor<T>
    {
        public T CurrentValue => options;

        public T Get(string? name) => options;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
