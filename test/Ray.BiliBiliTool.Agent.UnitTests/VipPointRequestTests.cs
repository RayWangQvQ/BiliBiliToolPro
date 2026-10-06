using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Ray.BiliBiliTool.Config.Options;
using Refit;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class VipPointRequestTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2Request_UsesAppHeadersAndCookieForm(bool complete)
    {
        var captured = new CaptureHandler();
        using var handler = new FormUrlEncodedKeyNormalizingDelegatingHandler
        {
            InnerHandler = new VipPointAppHeadersDelegatingHandler(new OptionsMonitor())
            {
                InnerHandler = captured,
            },
        };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.bilibili.com"),
        };
        client.DefaultRequestHeaders.Add("User-Agent", "test-web-agent");
        var api = RestService.For<IApiApi>(client);
        var request = new VipPointV2TaskRequest("dress-view") { Csrf = "test-csrf", Ts = 12345 };
        var response = complete
            ? await api.VipBigPointCompleteV2(request, "test-cookie")
            : await api.VipBigPointReceiveV2(request, "test-cookie");
        Assert.Equal(0, response.Code);
        Assert.Contains("taskCode=dress-view", captured.Body);
        Assert.Contains("csrf=test-csrf", captured.Body);
        Assert.Contains("ts=12345", captured.Body);
        Assert.Contains("mobi_app=android", captured.Body);
        Assert.Equal("test-app-agent build/9120300", captured.Headers["User-Agent"]);
        Assert.Contains("build=9120300", captured.Body);
        Assert.Equal("android64", captured.Headers["app-key"]);
        Assert.Equal("h5", captured.Headers["native_api_from"]);
        Assert.Equal("test-cookie", captured.Headers["Cookie"]);
        Assert.Equal("application/x-www-form-urlencoded", captured.MediaType);
    }

    [Fact]
    public async Task WebRequest_PreservesWebUserAgent()
    {
        var captured = new CaptureHandler();
        using var handler = new VipPointAppHeadersDelegatingHandler(new OptionsMonitor())
        {
            InnerHandler = captured,
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.bilibili.com/x/web-interface/nav"
        );
        request.Headers.Add("User-Agent", "test-web-agent");
        using var response = await client.SendAsync(request);
        Assert.Equal("test-web-agent", captured.Headers["User-Agent"]);
        Assert.False(captured.Headers.ContainsKey("app-key"));
    }

    private sealed class OptionsMonitor : IOptionsMonitor<SecurityOptions>
    {
        public SecurityOptions CurrentValue =>
            new() { UserAgentApp = "test-app-agent build/9120300" };

        public SecurityOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<SecurityOptions, string?> listener) => null;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = "";
        public string? MediaType { get; private set; }
        public Dictionary<string, string> Headers { get; private set; } =
            new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Headers = request.Headers.ToDictionary(
                header => header.Key,
                header => string.Join(",", header.Value),
                StringComparer.OrdinalIgnoreCase
            );
            Headers["User-Agent"] = request.Headers.UserAgent.ToString();
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            MediaType = request.Content?.Headers.ContentType?.MediaType;
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"code\":0}") };
        }
    }
}
