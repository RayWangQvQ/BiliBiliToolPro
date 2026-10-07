using System.Net;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Infrastructure.Cookies;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Infrastructure.UnitTests;

public class CookieExpiryNotificationTests
{
    [Theory]
    [InlineData("SCT123synthetic", "https://sctapi.ftqq.com/SCT123synthetic.send")]
    [InlineData("sctp123tsynthetic", "https://123.push.ft07.com/send/sctp123tsynthetic.send")]
    public void Endpoint_UsesOfficialHostForKeyType(string key, string expected)
    {
        Assert.Equal(expected, ServerChanCookieExpiryNotifier.CreateEndpoint(key)!.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://invalid.example/")]
    [InlineData("SCTexample/../../private")]
    [InlineData("sctpnotanumbertsecret")]
    public void Endpoint_InvalidKeysAreRejected(string? key)
    {
        Assert.Null(ServerChanCookieExpiryNotifier.CreateEndpoint(key));
    }

    [Theory]
    [InlineData("{\"code\":0}", true)]
    [InlineData("{\"code\":40001}", false)]
    [InlineData("{}", false)]
    public async Task Send_RequiresProviderAcknowledgementAndMasksAccount(
        string reply,
        bool delivered
    )
    {
        var handler = new CaptureHandler(reply);
        using var factory = new FakeFactory(handler);
        var config = Config(new() { ["CookieCheck:ServerChanSendKey"] = "SCT123synthetic" });
        var notifier = new ServerChanCookieExpiryNotifier(factory, config);
        Assert.Equal(delivered, await notifier.SendAsync("***3456", default));
        Assert.Equal(HttpMethod.Post, handler.Method);
        var decoded = Uri.UnescapeDataString(handler.Body!);
        Assert.Contains("***3456", decoded);
        Assert.DoesNotContain("SESSDATA", decoded);
        Assert.DoesNotContain("SCT123synthetic", decoded);
    }

    [Fact]
    public async Task DisabledOrUnconfiguredNotification_DoesNotSendHttp()
    {
        var handler = new CaptureHandler("{\"code\":0}");
        using var factory = new FakeFactory(handler);
        var disabled = Config(
            new()
            {
                ["CookieCheck:ServerChanSendKey"] = "SCT123synthetic",
                ["CookieCheck:NotifyEnabled"] = "false",
            }
        );
        Assert.False(
            await new ServerChanCookieExpiryNotifier(factory, disabled).SendAsync(
                "***3456",
                default
            )
        );
        Assert.False(
            await new ServerChanCookieExpiryNotifier(factory, Config(new())).SendAsync(
                "***3456",
                default
            )
        );
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void ExistingServerChanConfiguration_IsReusedWithoutFixedSinkIndex()
    {
        var config = Config(
            new()
            {
                ["Serilog:WriteTo:9:Name"] = "ServerChanBatched",
                ["Serilog:WriteTo:9:Args:turboScKey"] = "SCT123synthetic",
            }
        );
        Assert.Equal("SCT123synthetic", ServerChanCookieExpiryNotifier.GetSendKey(config));
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private sealed class CaptureHandler(string reply) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Body { get; private set; }
        public HttpMethod? Method { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Calls++;
            Method = request.Method;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(reply),
            };
        }
    }

    private sealed class FakeFactory(HttpMessageHandler handler) : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _client = new(handler);

        public HttpClient CreateClient(string name) => _client;

        public void Dispose() => _client.Dispose();
    }
}
