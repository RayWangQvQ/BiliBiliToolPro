using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class BiliAccountProbeTests
{
    [Fact]
    public async Task ProbeAsync_EmptyDeletedSlots_DoNotPreventValidAccountDetection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "",
                    ["BiliBiliCookies:1"] = " ",
                    ["BiliBiliCookies:2"] = "DedeUserID=98765; SESSDATA=test;",
                }
            )
            .Build();
        var service = new FakeAccountService();
        var probe = new BiliAccountProbe(
            new CookieStrFactory<BiliCookie>(config),
            service,
            NullLogger<BiliAccountProbe>.Instance
        );

        var result = await probe.ProbeAsync(98765, force: true);

        Assert.True(result.Success);
        Assert.Equal(1, service.LoginCalls);
        Assert.Equal("98765", service.LastUserId);
    }

    [Fact]
    public async Task ProbeAsync_ConfigurationFailure_ReturnsFailureInsteadOfThrowing()
    {
        var config = new ConfigurationBuilder().Add(new FailingConfigurationSource()).Build();
        var service = new FakeAccountService();
        var probe = new BiliAccountProbe(
            new CookieStrFactory<BiliCookie>(config),
            service,
            NullLogger<BiliAccountProbe>.Instance
        );

        var result = await probe.ProbeAsync(98766, force: true);

        Assert.False(result.Success);
        Assert.StartsWith("检测失败", result.Message);
        Assert.Equal(0, service.LoginCalls);
    }

    [Fact]
    public async Task ProbeAsync_NoActiveCookies_ReturnsFailureWithoutCallingApi()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BiliBiliCookies:0"] = "" })
            .Build();
        var service = new FakeAccountService();
        var probe = new BiliAccountProbe(
            new CookieStrFactory<BiliCookie>(config),
            service,
            NullLogger<BiliAccountProbe>.Instance
        );

        var result = await probe.ProbeAsync(98767, force: true);

        Assert.False(result.Success);
        Assert.Equal("未找到该账号的 Cookie", result.Message);
        Assert.Equal(0, service.LoginCalls);
    }

    private sealed class FailingConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new FailingConfigurationProvider();
    }

    private sealed class FailingConfigurationProvider : ConfigurationProvider
    {
        public override bool TryGet(string key, out string? value)
        {
            value = null;
            throw new InvalidOperationException("Configuration unavailable");
        }
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
