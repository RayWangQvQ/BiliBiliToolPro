using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService;
using Xunit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveDeviceInitializationTests
{
    private sealed class Builder : HttpMessageHandlerBuilder
    {
        public override string Name { get; set; } = "synthetic";
        public override HttpMessageHandler PrimaryHandler { get; set; } = new HttpClientHandler();
        public override IList<DelegatingHandler> AdditionalHandlers { get; } =
            new List<DelegatingHandler>();

        public override HttpMessageHandler Build() => PrimaryHandler;
    }

    [Theory]
    [InlineData(typeof(ILiveApi))]
    [InlineData(typeof(ILiveTraceApi))]
    public void LiveClientDoesNotShareImplicitCookiesBetweenAccounts(Type clientType)
    {
        var services = new ServiceCollection();
        var method = typeof(Ray.BiliBiliTool.Agent.Extensions.ServiceCollectionExtension)
            .GetMethods(
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            )
            .Single(method =>
                method.Name == "AddBiliBiliClientApi" && method.IsGenericMethodDefinition
            );
        method
            .MakeGenericMethod(clientType)
            .Invoke(
                null,
                [
                    services,
                    "https://api.live.bilibili.com",
                    (Action<IServiceProvider, HttpClient>)((_, _) => { }),
                    false,
                    null,
                ]
            );
        using var provider = services.BuildServiceProvider();
        var configurations = provider
            .GetServices<IConfigureOptions<HttpClientFactoryOptions>>()
            .OfType<ConfigureNamedOptions<HttpClientFactoryOptions>>()
            .Where(item => item.Name?.Contains(clientType.Name) == true)
            .ToArray();
        Assert.NotEmpty(configurations);
        var options = new HttpClientFactoryOptions();
        foreach (var configuration in configurations)
            configuration.Configure(configuration.Name, options);
        var builder = new Builder();
        options.HttpMessageHandlerBuilderActions.Last()(builder);
        using var handler = Assert.IsType<HttpClientHandler>(builder.PrimaryHandler);
        Assert.False(handler.UseCookies);
    }

    private sealed class Monitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private static HttpResponseMessage Response(params string[] cookies)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"code\":0}"),
        };
        if (cookies.Length > 0)
            response.Headers.TryAddWithoutValidation("set-cookie", cookies);
        return response;
    }

    private sealed class Environment
    {
        public BiliCookie Cookie = new(
            new()
            {
                ["DedeUserID"] = "1",
                ["SESSDATA"] = "synthetic-login",
                ["bili_jct"] = "synthetic-csrf",
            }
        );
        public readonly List<string> InitializationCookies = [];
        public int TaskReads;
        public Func<int, HttpResponseMessage> Initialize = _ => Response();
        public readonly LiveDomainService Service;

        public Environment()
        {
            var api = LiveFansMedalTaskTests.Proxy.Create<ILiveApi>(
                (method, args) =>
                {
                    if (method == "GetLiveHome")
                    {
                        InitializationCookies.Add((string)args[0]!);
                        return Task.FromResult(Initialize(InitializationCookies.Count));
                    }
                    Assert.Equal("GetActivatedMedalInfo", method);
                    TaskReads++;
                    Assert.Contains("SESSDATA=synthetic-login", (string)args[2]!);
                    return Task.FromResult(
                        new BiliApiResponse<ActivatedMedalResponse>
                        {
                            Code = 0,
                            Data = new() { Is_lighted = true, Reach_free_intimacy_limit = true },
                        }
                    );
                }
            );
            Service = new(
                NullLogger<LiveDomainService>.Instance,
                api,
                null!,
                null!,
                new Monitor<DailyTaskOptions>(new()),
                new Monitor<LiveLotteryTaskOptions>(new()),
                new Monitor<LiveFansMedalTaskOptions>(new() { IsEnable = true }),
                new Monitor<SecurityOptions>(new()),
                new Monitor<Silver2CoinTaskOptions>(new())
            );
        }
    }

    [Theory]
    [InlineData("like")]
    [InlineData("sendDanmu")]
    public async Task InteractionsWithoutLiveDeviceCookieDoNotInitializeWatching(string action)
    {
        var env = new Environment();
        await env.Service.RunFansMedalActionForAnchorAsync(env.Cookie, 2, 3, action);
        Assert.Empty(env.InitializationCookies);
        Assert.Equal(1, env.TaskReads);
    }

    [Fact]
    public async Task WatchingRetriesMissingHeaderAnonymouslyAndPreservesCredentials()
    {
        var env = new Environment();
        env.Initialize = attempt =>
            attempt == 1
                ? Response()
                : Response(
                    "LIVE_BUVID=synthetic-device; Path=/; Secure; HttpOnly",
                    "SESSDATA=must-not-replace; Path=/",
                    "bili_jct=must-not-replace; Path=/"
                );
        await env.Service.RunFansMedalActionForAnchorAsync(env.Cookie, 2, 3, "watchLive");
        Assert.Equal(2, env.InitializationCookies.Count);
        Assert.Contains("SESSDATA=synthetic-login", env.InitializationCookies[0]);
        Assert.Empty(env.InitializationCookies[1]);
        Assert.Equal("synthetic-device", env.Cookie.LiveBuvid);
        Assert.Equal("synthetic-login", env.Cookie.SessData);
        Assert.Equal("synthetic-csrf", env.Cookie.BiliJct);
        Assert.Equal(4, env.Cookie.CookieItemDictionary.Count);
        Assert.Equal(1, env.TaskReads);
    }

    [Fact]
    public async Task WatchingMergesDeviceHeaderWithoutCookieAttributes()
    {
        var env = new Environment();
        env.Initialize = _ => Response("LIVE_BUVID=synthetic-device; Path=/; Domain=.bilibili.com");
        await env.Service.RunFansMedalActionForAnchorAsync(env.Cookie, 2, 3, "watchLive");
        Assert.Single(env.InitializationCookies);
        Assert.Equal("synthetic-device", env.Cookie.LiveBuvid);
        Assert.DoesNotContain("Domain", env.Cookie.CookieItemDictionary.Keys);
    }

    [Fact]
    public async Task WatchingWithExistingDeviceSkipsInitialization()
    {
        var env = new Environment();
        env.Cookie.MergeCurrentCookie("LIVE_BUVID=existing-device");
        await env.Service.RunFansMedalActionForAnchorAsync(env.Cookie, 2, 3, "watchLive");
        Assert.Empty(env.InitializationCookies);
        Assert.Equal(1, env.TaskReads);
    }

    [Fact]
    public async Task WatchingStopsAfterTwoMissingHeadersWithoutStartingHeartbeat()
    {
        var env = new Environment();
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() =>
            env.Service.RunFansMedalActionForAnchorAsync(env.Cookie, 2, 3, "watchLive")
        );
        Assert.Equal("直播设备信息暂未获取，观看任务稍后重试", error.Message);
        Assert.Equal(2, env.InitializationCookies.Count);
        Assert.Equal(0, env.TaskReads);
        Assert.Equal("synthetic-login", env.Cookie.SessData);
    }

    [Fact]
    public async Task CancellationStopsBeforeDeviceInitialization()
    {
        var env = new Environment();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            env.Service.RunFansMedalActionForAnchorAsync(
                env.Cookie,
                2,
                3,
                "watchLive",
                cancellation.Token
            )
        );
        Assert.Empty(env.InitializationCookies);
        Assert.Equal(0, env.TaskReads);
    }

    [Theory]
    [InlineData("发弹幕")]
    [InlineData("发送弹幕")]
    public void NumberlessPlatformDanmakuTitleUsesOneMessagePerRound(string title)
    {
        var plan = LiveFansMedalTaskPlanner.Plan(
            new()
            {
                Is_lighted = true,
                Task_info =
                [
                    new()
                    {
                        Jump_type = "sendDanmu",
                        Title = title,
                        Sub_title = "每日上限 3/10",
                    },
                ],
            },
            "sendDanmu"
        );
        Assert.Equal(7, plan.Remaining);
        Assert.Equal(1, plan.RoundSize);
        Assert.Equal(3, plan.Completed);
    }
}
