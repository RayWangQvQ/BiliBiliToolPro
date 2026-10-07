using System.Net;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Xunit;

namespace Ray.BiliBiliTool.Infrastructure.UnitTests;

public class TaskFailureNotificationTests
{
    [Theory]
    [InlineData("{\"code\":0}", true)]
    [InlineData("{\"code\":1}", false)]
    [InlineData("{}", false)]
    public async Task Summary_UsesOneRequestAndRequiresAcknowledgement(string reply, bool accepted)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CookieCheck:ServerChanSendKey"] = "SCT123synthetic",
                }
            )
            .Build();
        var handler = new CaptureHandler(reply);
        using var factory = new Factory(handler);
        var notifier = new ServerChanTaskFailureNotifier(factory, config);
        var summary = new TaskFailureSummary(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [new("每日任务", ["***1234"], 2), new("漫画签到/阅读", ["***4321"], 1)]
        );
        Assert.Equal(accepted, await notifier.SendAsync(summary, default));
        Assert.Equal(1, handler.Calls);
        var body = Uri.UnescapeDataString(handler.Body!.Replace("+", " "));
        Assert.Contains("每日任务", body);
        Assert.Contains("漫画签到/阅读", body);
        Assert.Contains("统一汇总提醒", body);
        Assert.DoesNotContain("SCT123synthetic", body);
        config["TaskFailureNotification:Enabled"] = "false";
        Assert.False(await notifier.SendAsync(summary, default));
        Assert.Equal(1, handler.Calls);
    }

    private sealed class CaptureHandler(string reply) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken token
        )
        {
            Calls++;
            Body = await request.Content!.ReadAsStringAsync(token);
            return new(HttpStatusCode.OK) { Content = new StringContent(reply) };
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _client = new(handler);

        public HttpClient CreateClient(string name) => _client;

        public void Dispose() => _client.Dispose();
    }
}
