using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class LiveMedalMonitorSourceTests
{
    private sealed class Guard : ICookieTaskGuard
    {
        public int Checks;
        public bool Expired;

        public Task EnsureValidAsync(
            string uid,
            string cookie,
            CancellationToken cancellationToken = default
        )
        {
            Checks++;
            return Expired
                ? Task.FromException(
                    new InvalidOperationException(
                        "Cookie 已过期，本次活动已跳过，请在账号管理中重新登录"
                    )
                )
                : Task.CompletedTask;
        }

        public Task CheckNowAsync(
            string uid,
            string cookie,
            CancellationToken cancellationToken = default
        ) => EnsureValidAsync(uid, cookie, cancellationToken);
    }

    private sealed class Dashboard : ILiveMedalDashboardService
    {
        public int Reads;

        public IReadOnlyList<LiveMedalAccount> GetAccounts() => [new(0, "示例账号")];

        public Task<LiveMedalSnapshot?> GetCachedAsync(
            int index,
            CancellationToken token = default
        ) => throw new NotSupportedException();

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        )
        {
            Reads++;
            Assert.True(refresh);
            return Task.FromResult(
                new LiveMedalSnapshot(
                    Enumerable
                        .Range(1, 5)
                        .Select(id => new LiveMedalCard(
                            id,
                            "示例主播",
                            "示例牌",
                            30,
                            false,
                            true,
                            false,
                            [new("like", "点赞30次", "每日上限 0/10", id == 4, 0)],
                            null,
                            RoomId: id
                        ))
                        .ToArray(),
                    DateTimeOffset.UtcNow
                )
            );
        }
    }

    private sealed class Environment : IDisposable
    {
        public readonly Guard Guard = new();
        public readonly Dashboard Dashboard = new();
        public int RoomReads;
        public int Runs;
        public readonly CookieStrFactory<BiliCookie> Cookies;
        public readonly ServiceProvider Services;
        public readonly LiveMedalMonitorSource Source;

        public Environment()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["BiliBiliCookies:0"] =
                            "DedeUserID=1;bili_jct=synthetic;SESSDATA=synthetic",
                    }
                )
                .Build();
            Cookies = new(config);
            var api = LiveTaskTestSupport.Proxy.Create<ILiveApi>(
                (method, args) =>
                {
                    Assert.Equal("GetLiveRoomInfo", method);
                    RoomReads++;
                    var room = (long)args[0]!;
                    if (room == 2)
                        return Task.FromException<BiliApiResponse<GetLiveRoomInfoResponse>>(
                            new IOException("synthetic error")
                        );
                    return Task.FromResult(
                        new BiliApiResponse<GetLiveRoomInfoResponse>
                        {
                            Code = 0,
                            Data = new() { Live_Status = room == 1 ? 1 : 0 },
                        }
                    );
                }
            );
            var domain = LiveTaskTestSupport.Proxy.Create<ILiveDomainService>(
                (method, args) =>
                {
                    Assert.Equal("RunFansMedalActionForAnchorAsync", method);
                    Assert.Equal("1", Assert.IsType<BiliCookie>(args[0]).UserId);
                    Assert.Equal(1L, args[1]);
                    Assert.Equal(1001L, args[2]);
                    Assert.Equal("like", args[3]);
                    Runs++;
                    return Task.CompletedTask;
                }
            );
            Services = new ServiceCollection()
                .AddSingleton(Cookies)
                .AddSingleton<ICookieTaskGuard>(Guard)
                .AddSingleton<ILiveMedalDashboardService>(Dashboard)
                .AddSingleton(api)
                .AddSingleton(domain)
                .BuildServiceProvider();
            Source = new(
                Services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<LiveMedalMonitorSource>.Instance
            );
        }

        public void Dispose() => Services.Dispose();
    }

    [Fact]
    public async Task RealRoomStateOverridesPanelStateAndOneFailedRoomDoesNotHideOtherAnchors()
    {
        using var env = new Environment();
        var account = Assert.Single(
            await env.Source.ReadAsync(new() { ExcludedAnchorIds = "5" }, CancellationToken.None)
        );
        Assert.Equal(3, env.RoomReads);
        Assert.True(account.Snapshot.Medals[0].Live);
        Assert.Equal("直播状态暂未获取", account.Snapshot.Medals[1].Error);
        Assert.False(account.Snapshot.Medals[2].Live);
        Assert.Equal(5, account.Snapshot.Medals.Count);
        Assert.Equal(1, env.Guard.Checks);
    }

    [Fact]
    public async Task ExpiredAccountsAreCheckedBeforeProgressQueriesOrInteractions()
    {
        using var env = new Environment();
        env.Guard.Expired = true;
        Assert.Empty(await env.Source.ReadAsync(new(), CancellationToken.None));
        Assert.Equal(0, env.Dashboard.Reads);
        Assert.Equal(0, env.RoomReads);
        var target = new LiveMedalMonitorTarget(
            0,
            1,
            LiveMedalMonitorSource.CredentialVersion(env.Cookies.GetCookie(0)),
            1,
            1001,
            "like"
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            env.Source.ExecuteAsync(target, CancellationToken.None)
        );
        Assert.Equal(0, env.Runs);
    }

    [Theory]
    [InlineData("index")]
    [InlineData("uid")]
    [InlineData("credential")]
    [InlineData("valid")]
    public async Task ExecutionRechecksAccountIdentityAndCredentials(string mode)
    {
        using var env = new Environment();
        var target = new LiveMedalMonitorTarget(
            mode == "index" ? 1 : 0,
            mode == "uid" ? 2 : 1,
            mode == "credential"
                ? "old"
                : LiveMedalMonitorSource.CredentialVersion(env.Cookies.GetCookie(0)),
            1,
            1001,
            "like"
        );
        await env.Source.ExecuteAsync(target, CancellationToken.None);
        Assert.Equal(mode == "valid" ? 1 : 0, env.Runs);
        Assert.Equal(mode == "valid" ? 1 : 0, env.Guard.Checks);
    }
}
