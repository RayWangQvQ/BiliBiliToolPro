using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class AccountDomainServiceTests
{
    [Theory]
    [InlineData(0, 100, 7)]
    [InlineData(1, 100, 4)]
    [InlineData(5, 100, 2)]
    [InlineData(5, 1000, 38)]
    [InlineData(5, 0, 0)]
    public void CalculateUpgradeTime_CoinAllowanceAndRemainingExp_UsesConfiguredDailyRate(
        int numberOfCoins,
        long remainingExp,
        int expectedDays
    )
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<DailyTaskOptions>(options =>
        {
            options.NumberOfCoins = numberOfCoins;
            options.IsWatchVideo = true;
            options.IsShareVideo = true;
        });
        services.Configure<UnfollowBatchedTaskOptions>(_ => { });
        using var provider = services.BuildServiceProvider();
        var service = new AccountDomainService(
            NullLogger<AccountDomainService>.Instance,
            null!,
            null!,
            provider.GetRequiredService<IOptionsMonitor<UnfollowBatchedTaskOptions>>(),
            provider.GetRequiredService<IOptionsMonitor<DailyTaskOptions>>()
        );
        var account = new UserInfo
        {
            Money = 7,
            Level_info = new LevelInfo
            {
                Current_level = 5,
                Current_exp = 0,
                Next_exp = remainingExp,
            },
            Wbi_img = new WbiImg { img_url = "", sub_url = "" },
        };

        Assert.Equal(expectedDays, service.CalculateUpgradeTime(account));
    }
}
