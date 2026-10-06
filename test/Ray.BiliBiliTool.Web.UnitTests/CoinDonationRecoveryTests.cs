using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class CoinDonationRecoveryTests
{
    [Theory]
    [InlineData(3, 1)]
    [InlineData(4, 0)]
    [InlineData(6, 0)]
    public async Task Recovery_RespectsAccountLevelBeforeDonating(int level, int expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "DedeUserID=1001;bili_jct=synthetic;SESSDATA=synthetic",
                    ["DailyTaskConfig:CoinDonationStopLevel"] = "4",
                }
            )
            .Build();
        var account = DispatchProxy.Create<IAccountDomainService, ApiProxy>();
        ((ApiProxy)account).Call = _ =>
            Task.FromResult(
                new UserInfo
                {
                    Wbi_img = new()
                    {
                        img_url = "https://example.com/wbi/test.png",
                        sub_url = "https://example.com/wbi/sub.png",
                    },
                    Level_info = new LevelInfo { Current_level = level },
                }
            );
        var donations = 0;
        var donate = DispatchProxy.Create<IDonateCoinDomainService, ApiProxy>();
        ((ApiProxy)donate).Call = _ =>
        {
            donations++;
            return Task.CompletedTask;
        };
        using var services = new ServiceCollection().BuildServiceProvider();
        var executor = new TaskRecoveryExecutor(
            new CookieStrFactory<BiliCookie>(config),
            config,
            account,
            null!,
            donate,
            null!,
            services,
            NullLogger<TaskRecoveryExecutor>.Instance,
            new AllowGuard()
        );
        var task = TaskCatalog.All.Single(t => t.TaskKey == "DailyTaskAppService");
        var execution = executor.ExecuteAsync(
            1001,
            task,
            task.Items.Single(item => item.ItemKey == "DonateCoin")
        );
        if (expected == 0)
            await Assert.ThrowsAsync<TaskRecoverySkippedException>(() => execution);
        else
            await execution;
        Assert.Equal(expected, donations);
    }

    private sealed class AllowGuard : ICookieTaskGuard
    {
        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken token = default
        ) => Task.CompletedTask;
    }

    public class ApiProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Call { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!);
    }
}
