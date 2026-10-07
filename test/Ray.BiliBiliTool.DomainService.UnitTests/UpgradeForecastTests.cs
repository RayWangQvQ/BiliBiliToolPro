using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class UpgradeForecastTests
{
    [Theory]
    [InlineData(0, 100, 100, true, true, 7)]
    [InlineData(5, 0, 100, true, true, 7)]
    [InlineData(5, 0, 2, false, false, 8)]
    [InlineData(1, 0, 0, false, false, 7)]
    [InlineData(5, 7.9, 7, false, false, 7)]
    [InlineData(0, 0, 0, false, false, 20)]
    public void UpgradeForecast_RespectsProtectionAndEnabledTasks(
        int coins,
        double balance,
        int protectedCoins,
        bool watch,
        bool share,
        int expected
    )
    {
        var service = new AccountDomainService(
            NullLogger<AccountDomainService>.Instance,
            null!,
            null!,
            new ForecastOptions<UnfollowBatchedTaskOptions>(new()),
            new ForecastOptions<DailyTaskOptions>(
                new()
                {
                    NumberOfCoins = coins,
                    NumberOfProtectedCoins = protectedCoins,
                    IsWatchVideo = watch,
                    IsShareVideo = share,
                }
            )
        );
        Assert.Equal(
            expected,
            service.CalculateUpgradeTime(
                new UserInfo
                {
                    Money = (decimal)balance,
                    Level_info = new()
                    {
                        Current_level = 5,
                        Current_exp = 0,
                        Next_exp = 100,
                    },
                    Wbi_img = new() { img_url = "", sub_url = "" },
                }
            )
        );
    }

    [Fact]
    public void UpgradeForecast_MaximumLevel_HasNoNextLevel()
    {
        var service = new AccountDomainService(
            NullLogger<AccountDomainService>.Instance,
            null!,
            null!,
            new ForecastOptions<UnfollowBatchedTaskOptions>(new()),
            new ForecastOptions<DailyTaskOptions>(new())
        );
        Assert.Equal(
            0,
            service.CalculateUpgradeTime(
                new UserInfo
                {
                    Level_info = new() { Current_level = 6, Next_exp = "--" },
                    Wbi_img = new() { img_url = "", sub_url = "" },
                }
            )
        );
    }

    private sealed class ForecastOptions<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
