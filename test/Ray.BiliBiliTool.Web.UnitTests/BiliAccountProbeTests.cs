using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class BiliAccountProbeTests
{
    [Fact]
    public async Task ForcedCheck_ConfirmedExpiryReturnsFailureWithoutRunningAccountAction()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "DedeUserID=98768; SESSDATA=synthetic",
                }
            )
            .Build();
        var service = new FakeAccountService();
        var probe = new BiliAccountProbe(
            new CookieStrFactory<BiliCookie>(config),
            service,
            NullLogger<BiliAccountProbe>.Instance,
            new ExpiredGuard()
        );
        var result = await probe.ProbeAsync(98768, force: true);
        Assert.False(result.Success);
        Assert.Contains("Cookie 已过期", result.Message);
        Assert.Equal(0, service.LoginCalls);
    }

    private sealed class ExpiredGuard : ICookieTaskGuard
    {
        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Cookie 已过期");
    }

    private sealed class AllowGuard : ICookieTaskGuard
    {
        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }

    private sealed class FakeAccountService : IAccountDomainService
    {
        public int LoginCalls { get; private set; }
        public string? LastUserId { get; private set; }

        public Task<UserInfo> LoginByCookie(BiliCookie cookie)
        {
            LoginCalls++;
            LastUserId = cookie.UserId;
            return Task.FromResult(
                new UserInfo
                {
                    IsLogin = true,
                    Uname = "test",
                    Wbi_img = null!,
                }
            );
        }

        public Task<DailyTaskInfo> GetDailyTaskStatus(BiliCookie ck) =>
            throw new NotSupportedException();

        public Task UnfollowBatched(BiliCookie ck) => throw new NotSupportedException();

        public int CalculateUpgradeTime(UserInfo userInfo) => throw new NotSupportedException();
    }
}
