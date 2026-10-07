using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using Ray.BiliBiliTool.Config.Options;
using Refit;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class RequestRetrySafetyTests
{
    [Theory]
    [InlineData(408)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    [InlineData(404)]
    public async Task MutatingPolicy_DoesNotReplayAnUnconfirmedWrite(int status)
    {
        var attempts = 0;
        using var response = await BiliResiliencePolicies
            .MutatingPolicy()
            .ExecuteAsync(() =>
            {
                attempts++;
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status));
            });
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task MutatingPolicy_DoesNotReplayAfterConnectionFailure()
    {
        var attempts = 0;
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            BiliResiliencePolicies
                .MutatingPolicy()
                .ExecuteAsync(() =>
                {
                    attempts++;
                    return Task.FromException<HttpResponseMessage>(
                        new HttpRequestException("synthetic")
                    );
                })
        );
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task MutatingPolicy_SuccessRunsOnce()
    {
        var attempts = 0;
        using var response = await BiliResiliencePolicies
            .MutatingPolicy()
            .ExecuteAsync(() =>
            {
                attempts++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
        Assert.Equal(1, attempts);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadOnlyPolicy_StillRetriesTransientFailuresOnce()
    {
        var attempts = 0;
        using var response = await BiliResiliencePolicies
            .ReadOnlyPolicy()
            .ExecuteAsync(() =>
            {
                attempts++;
                return Task.FromResult(
                    new HttpResponseMessage(
                        attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK
                    )
                );
            });
        Assert.Equal(2, attempts);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", 2)]
    [InlineData("HEAD", 2)]
    [InlineData("OPTIONS", 2)]
    [InlineData("POST", 1)]
    [InlineData("PUT", 1)]
    [InlineData("DELETE", 1)]
    [InlineData("PATCH", 1)]
    [InlineData("CUSTOM", 1)]
    public async Task RequestPolicy_SelectsRetriesOnlyForReadMethods(
        string method,
        int expectedAttempts
    )
    {
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            "https://synthetic.invalid"
        );
        var attempts = 0;
        using var response = await BiliResiliencePolicies
            .ForRequest(request)
            .ExecuteAsync(() =>
            {
                attempts++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            });
        Assert.Equal(expectedAttempts, attempts);
    }

    [Theory]
    [InlineData("qinglong-add", 1)]
    [InlineData("qinglong-update", 1)]
    [InlineData("qinglong-token", 2)]
    [InlineData("vip-receive", 1)]
    [InlineData("vip-combine", 2)]
    [InlineData("manga-sign", 1)]
    [InlineData("live-sign", 1)]
    [InlineData("silver-exchange", 1)]
    [InlineData("web-heartbeat", 1)]
    [InlineData("live-wallet", 2)]
    public async Task RegisteredClient_OnlyReadRequestsRetry(string operation, int expectedAttempts)
    {
        var capture = new UnavailableHandler();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.Configure<SecurityOptions>(options =>
        {
            options.IntervalSecondsBetweenRequestApi = 0;
            options.UserAgent = "synthetic-agent";
        });
        services.AddBiliBiliClientApi(configuration);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder =>
                builder.PrimaryHandler = capture
            )
        );
        using var provider = services.BuildServiceProvider();
        var error = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            switch (operation)
            {
                case "qinglong-add":
                    await provider
                        .GetRequiredService<IQingLongApi>()
                        .AddEnvsAsync(
                            [new AddQingLongEnv { name = "synthetic", value = "synthetic" }],
                            "Bearer synthetic"
                        );
                    break;
                case "qinglong-update":
                    await provider
                        .GetRequiredService<IQingLongApi>()
                        .UpdateEnvsAsync(
                            new UpdateQingLongEnv
                            {
                                id = 1,
                                name = "synthetic",
                                value = "synthetic",
                            },
                            "Bearer synthetic"
                        );
                    break;
                case "qinglong-token":
                    await provider
                        .GetRequiredService<IQingLongApi>()
                        .GetTokenAsync("synthetic", "synthetic");
                    break;
                case "vip-receive":
                    await provider
                        .GetRequiredService<IApiApi>()
                        .VipBigPointReceiveV2(
                            new VipPointV2TaskRequest("dress-view") { Csrf = "synthetic", Ts = 1 },
                            "synthetic"
                        );
                    break;
                case "vip-combine":
                    await provider
                        .GetRequiredService<IApiApi>()
                        .GetCombineAsync(
                            new GetCombineRequest { csrf = "synthetic", buvid = "synthetic" },
                            "synthetic"
                        );
                    break;
                case "manga-sign":
                    await provider.GetRequiredService<IMangaApi>().ClockIn("android", "synthetic");
                    break;
                case "live-sign":
                    await provider.GetRequiredService<ILiveApi>().Sign("synthetic");
                    break;
                case "silver-exchange":
#pragma warning disable CS0612
                    await provider.GetRequiredService<ILiveApi>().ExchangeSilver2Coin("synthetic");
#pragma warning restore CS0612
                    break;
                case "web-heartbeat":
                    await provider
                        .GetRequiredService<ILiveTraceApi>()
                        .WebHeartBeat(new WebHeartBeatRequest(1, 60), "synthetic");
                    break;
                case "live-wallet":
                    await provider.GetRequiredService<ILiveApi>().GetLiveWalletStatus("synthetic");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal(expectedAttempts, capture.Attempts);
    }

    private sealed class UnavailableHandler : HttpMessageHandler
    {
        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Attempts++;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    RequestMessage = request,
                    Content = new StringContent("{\"code\":500,\"message\":\"synthetic\"}"),
                }
            );
        }
    }
}
