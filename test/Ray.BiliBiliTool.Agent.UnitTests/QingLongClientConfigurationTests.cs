using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using Ray.BiliBiliTool.Config.Options;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class QingLongClientConfigurationTests
{
    [Theory]
    [InlineData(null, "http://localhost:5700")]
    [InlineData("", "http://localhost:5700")]
    [InlineData("  ", "http://localhost:5700")]
    [InlineData("http://localhost:5600", "http://localhost:5600")]
    [InlineData("http://localhost:5700", "http://localhost:5700")]
    [InlineData("https://qinglong.test.invalid:7443", "https://qinglong.test.invalid:7443")]
    [InlineData("  http://localhost:5600/  ", "http://localhost:5600")]
    public async Task Client_UsesConfiguredOrDefaultEndpointForAllOpenApiRequests(
        string? configured,
        string expected
    )
    {
        var handler = new CaptureHandler(new Uri(expected));
        using var services = BuildServices(configured, handler);
        var api = services.GetRequiredService<IQingLongApi>();
        var authentication = await api.GetTokenAsync("synthetic-client", "synthetic-secret");
        Assert.Equal(200, authentication.Code);
        var authorization = authentication.Data.token_type + " " + authentication.Data.token;
        var envs = await api.GetEnvsAsync("synthetic-variable", authorization);
        Assert.Empty(envs.Data);
        var added = await api.AddEnvsAsync(
            [new AddQingLongEnv { name = "synthetic-variable", value = "synthetic-value" }],
            authorization
        );
        Assert.Equal(200, added.Code);
        var updated = await api.UpdateEnvsAsync(
            new UpdateQingLongEnv
            {
                id = 1,
                name = "synthetic-variable",
                value = "synthetic-updated-value",
            },
            authorization
        );
        Assert.Equal(200, updated.Code);
        Assert.Equal("synthetic-updated-value", updated.Data.value);
        Assert.Equal(4, handler.Requests);
    }

    [Theory]
    [InlineData("localhost:5700")]
    [InlineData("not a URI")]
    [InlineData("ftp://localhost:5700")]
    [InlineData("file:///synthetic")]
    public void InvalidEndpoint_ExplainsConfigurationWithoutEchoingItsValue(string configured)
    {
        using var services = BuildServices(
            configured,
            new CaptureHandler(new Uri("http://localhost:5700"))
        );
        var exception = Assert.Throws<UriFormatException>(() =>
            services.GetRequiredService<IQingLongApi>()
        );
        Assert.Contains("QL_URL", exception.Message);
        Assert.DoesNotContain(configured, exception.Message);
    }

    private static ServiceProvider BuildServices(string? configured, CaptureHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QL_URL"] = configured })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.Configure<SecurityOptions>(options => options.UserAgent = "synthetic-agent");
        services.AddBiliBiliClientApi(configuration);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder =>
                builder.PrimaryHandler = handler
            )
        );
        return services.BuildServiceProvider();
    }

    private sealed class CaptureHandler(Uri expected) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            var uri = request.RequestUri!;
            Assert.Equal(
                expected.GetLeftPart(UriPartial.Authority),
                uri.GetLeftPart(UriPartial.Authority)
            );
            string payload;
            if (uri.AbsolutePath == "/open/auth/token")
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Contains("client_id=synthetic-client", uri.Query);
                Assert.Contains("client_secret=synthetic-secret", uri.Query);
                payload =
                    "{\"code\":200,\"data\":{\"token\":\"synthetic-token\",\"token_type\":\"Bearer\"}}";
            }
            else
            {
                Assert.Equal("/open/envs", uri.AbsolutePath);
                Assert.Equal("Bearer synthetic-token", request.Headers.Authorization!.ToString());
                payload =
                    request.Method == HttpMethod.Put
                        ? "{\"code\":200,\"data\":{\"id\":1,\"name\":\"synthetic-variable\",\"value\":\"synthetic-updated-value\",\"timestamp\":\"synthetic-time\"}}"
                        : "{\"code\":200,\"data\":[]}";
            }
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                }
            );
        }
    }
}
