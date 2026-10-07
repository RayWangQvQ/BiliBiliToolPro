using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace Ray.BiliBiliTool.Application.UnitTests;

public class CookieTaskGuardTests
{
    private const string Cookie = "DedeUserID=123456; SESSDATA=synthetic";

    [Fact]
    public async Task ManualMode_SkipsAutomaticCheckButExplicitCheckDetectsExpiry()
    {
        var fixture = new Fixture();
        fixture.Configuration["CookieCheck:AutoCheckEnabled"] = "false";
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        Assert.Equal(0, fixture.Api.Calls);
        await fixture.Guard.CheckNowAsync("123456", Cookie);
        fixture.Api.Response = new() { Code = -101 };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.CheckNowAsync("123456", Cookie)
        );
        Assert.Equal(2, fixture.Api.Calls);
        Assert.Equal(1, fixture.Notifier.Calls);
    }

    [Fact]
    public async Task EnablingAutomaticMode_TakesEffectWithoutRestart()
    {
        var fixture = new Fixture();
        fixture.Configuration["CookieCheck:AutoCheckEnabled"] = "false";
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        fixture.Configuration["CookieCheck:AutoCheckEnabled"] = "true";
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        Assert.Equal(1, fixture.Api.Calls);
    }

    [Fact]
    public async Task LocalMidnight_TriggersNewCheckBeforeUtcDateChanges()
    {
        var fixture = new Fixture();
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        fixture.Clock.Now += TimeSpan.FromMinutes(2);
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        Assert.Equal(2, fixture.Api.Calls);
    }

    [Fact]
    public async Task ValidCookie_IsCheckedOnceAcrossTasksAndRestartsEachLocalDay()
    {
        var fixture = new Fixture();
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        await fixture.CreateGuard().EnsureValidAsync("123456", Cookie);
        Assert.Equal(1, fixture.Api.Calls);
        fixture.Clock.Now += TimeSpan.FromDays(1);
        await fixture.Guard.EnsureValidAsync("123456", Cookie);
        Assert.Equal(2, fixture.Api.Calls);
        Assert.Equal(0, fixture.Notifier.Calls);
    }

    [Fact]
    public async Task ChangedCookie_IsRecheckedImmediatelyAndResumesActivity()
    {
        var fixture = new Fixture();
        fixture.Api.Response = new() { Code = -101 };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        fixture.Api.Response = new()
        {
            Code = 0,
            Data = new UserInfo { IsLogin = true, Wbi_img = null! },
        };
        await fixture.Guard.EnsureValidAsync("123456", Cookie + "-renewed");
        Assert.Equal(2, fixture.Api.Calls);
        Assert.Equal(1, fixture.Notifier.Calls);
    }

    [Theory]
    [InlineData(-101)]
    [InlineData(0)]
    public async Task ConfirmedExpiry_BlocksTasksAndNotifiesOnlyOncePerDay(int code)
    {
        var fixture = new Fixture();
        fixture.Api.Response = new()
        {
            Code = code,
            Data = new UserInfo { IsLogin = false, Wbi_img = null! },
        };
        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.CreateGuard().EnsureValidAsync("123456", Cookie)
            );
        Assert.Equal(1, fixture.Api.Calls);
        Assert.Equal(1, fixture.Notifier.Calls);
        Assert.Equal("***3456", fixture.Notifier.LastAccount);
        fixture.Clock.Now += TimeSpan.FromDays(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        Assert.Equal(2, fixture.Api.Calls);
        Assert.Equal(2, fixture.Notifier.Calls);
    }

    [Theory]
    [InlineData(-412)]
    [InlineData(-500)]
    [InlineData(0)]
    public async Task UnconfirmedResponse_DoesNotSendExpiryAlertOrCacheFailure(int code)
    {
        var fixture = new Fixture();
        fixture.Api.Response = new() { Code = code };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        Assert.Contains("暂时失败", error.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        Assert.Equal(2, fixture.Api.Calls);
        Assert.Equal(0, fixture.Notifier.Calls);
        Assert.Null(await fixture.Store.ReadAsync("123456", default));
    }

    [Fact]
    public async Task NetworkFailure_DoesNotLeakExceptionOrSendExpiryAlert()
    {
        var fixture = new Fixture();
        fixture.Api.Error = new HttpRequestException("synthetic-private-cookie");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        Assert.DoesNotContain("synthetic-private-cookie", error.ToString());
        Assert.Equal(0, fixture.Notifier.Calls);
    }

    [Fact]
    public async Task FailedPush_IsRetriedAfterDelayAndNotMarkedAsDelivered()
    {
        var fixture = new Fixture();
        fixture.Api.Response = new() { Code = -101 };
        fixture.Notifier.Delivered = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.CreateGuard().EnsureValidAsync("123456", Cookie)
        );
        Assert.Equal(1, fixture.Notifier.Calls);
        Assert.Null((await fixture.Store.ReadAsync("123456", default))!.NotifiedDate);
        fixture.Clock.Now += TimeSpan.FromMinutes(16);
        fixture.Notifier.Delivered = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie)
        );
        Assert.Equal(2, fixture.Notifier.Calls);
        Assert.NotNull((await fixture.Store.ReadAsync("123456", default))!.NotifiedDate);
    }

    [Fact]
    public async Task ConcurrentTasks_ShareOneCheckAndOneNotification()
    {
        var fixture = new Fixture();
        fixture.Api.Response = new() { Code = -101 };
        await Task.WhenAll(
            Enumerable
                .Range(0, 10)
                .Select(_ =>
                    Assert.ThrowsAsync<InvalidOperationException>(() =>
                        fixture.Guard.EnsureValidAsync("123456", Cookie)
                    )
                )
        );
        Assert.Equal(1, fixture.Api.Calls);
        Assert.Equal(1, fixture.Notifier.Calls);
    }

    [Fact]
    public async Task CallerCancellation_IsPropagatedWithoutAlert()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Guard.EnsureValidAsync("123456", Cookie, cancellation.Token)
        );
        Assert.Equal(0, fixture.Api.Calls);
        Assert.Equal(0, fixture.Notifier.Calls);
    }

    [Fact]
    public async Task ExpiredAccount_IsSkippedWhileOtherAccountsContinue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "DedeUserID=1; SESSDATA=synthetic-one",
                    ["BiliBiliCookies:1"] = "DedeUserID=2; SESSDATA=synthetic-two",
                }
            )
            .Build();
        var guard = new SelectiveGuard();
        var activity = new RecordingActivity(configuration, guard);
        await activity.DoTaskAsync();
        Assert.Equal(new[] { "1", "2" }, guard.Accounts);
        Assert.Equal(new[] { "2" }, activity.Accounts);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            activity.DoTaskForAccountAsync(1)
        );
        Assert.Equal(new[] { "2" }, activity.Accounts);
    }

    private sealed class RecordingActivity(IConfiguration config, ICookieTaskGuard guard)
        : BaseMultiAccountsAppService(
            NullLogger.Instance,
            new CookieStrFactory<BiliCookie>(config),
            null!,
            config,
            guard
        )
    {
        public List<string> Accounts { get; } = [];

        protected override Task DoTaskAccountAsync(
            BiliCookie ck,
            CancellationToken cancellationToken = default
        )
        {
            Accounts.Add(ck.UserId);
            return Task.CompletedTask;
        }
    }

    private sealed class SelectiveGuard : ICookieTaskGuard
    {
        public List<string> Accounts { get; } = [];

        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken cancellationToken = default
        )
        {
            Accounts.Add(userId);
            if (userId == "1")
                throw new InvalidOperationException("Cookie 已过期");
            return Task.CompletedTask;
        }
    }

    private sealed class Fixture
    {
        public FakeNavApi Api { get; } = new();
        public MemoryStore Store { get; } = new();
        public FakeNotifier Notifier { get; } = new();
        public FakeClock Clock { get; } = new();
        public IConfigurationRoot Configuration { get; } =
            new ConfigurationBuilder().AddInMemoryCollection().Build();
        public CookieTaskGuard Guard { get; }

        public Fixture() => Guard = CreateGuard();

        public CookieTaskGuard CreateGuard() =>
            new(Api, Store, Notifier, Clock, NullLogger<CookieTaskGuard>.Instance, Configuration);
    }

    private sealed class FakeNavApi : INavApi
    {
        public int Calls { get; private set; }
        public Exception? Error { get; set; }
        public BiliApiResponse<UserInfo> Response { get; set; } =
            new()
            {
                Code = 0,
                Data = new UserInfo { IsLogin = true, Wbi_img = null! },
            };

        public Task<BiliApiResponse<UserInfo>> GetNavAsync(
            string ck,
            CancellationToken cancellationToken = default
        )
        {
            Calls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Error is null
                ? Task.FromResult(Response)
                : Task.FromException<BiliApiResponse<UserInfo>>(Error);
        }
    }

    private sealed class MemoryStore : ICookieCheckStateStore
    {
        private readonly Dictionary<string, CookieCheckState> _states = [];

        public Task<CookieCheckState?> ReadAsync(
            string userId,
            CancellationToken cancellationToken
        ) => Task.FromResult(_states.GetValueOrDefault(userId));

        public Task WriteAsync(
            string userId,
            CookieCheckState state,
            CancellationToken cancellationToken
        )
        {
            _states[userId] = state;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotifier : ICookieExpiryNotifier
    {
        public int Calls { get; private set; }
        public bool Delivered { get; set; } = true;
        public string? LastAccount { get; private set; }

        public Task<bool> SendAsync(string maskedAccount, CancellationToken cancellationToken)
        {
            Calls++;
            LastAccount = maskedAccount;
            return Task.FromResult(Delivered);
        }
    }

    private sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 15, 59, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone(
                "TestZone",
                TimeSpan.FromHours(8),
                "TestZone",
                "TestZone"
            );
    }
}
