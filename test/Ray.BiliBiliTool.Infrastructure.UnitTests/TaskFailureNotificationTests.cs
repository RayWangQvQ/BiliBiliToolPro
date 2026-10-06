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

    [Fact]
    public async Task PendingBatch_PersistsAcrossInstances_AndCanBeClearedAfterDelivery()
    {
        var directory = Directory.CreateTempSubdirectory("failure-state-test-");
        try
        {
            var path = Path.Combine(directory.FullName, "state.json");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["TaskFailureNotification:StateFile"] = path }
                )
                .Build();
            var store = new FileTaskFailureBatchStateStore(config);
            var state = new TaskFailureBatchState(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                [new("DailyTaskAppService", "***1234", 2)]
            );
            await store.WriteAsync(state, default);
            var restored = await new FileTaskFailureBatchStateStore(config).ReadAsync(default);
            Assert.Equal(2, Assert.Single(restored!.Entries).Count);
            Assert.DoesNotContain("SESSDATA", await File.ReadAllTextAsync(path));
            await store.WriteAsync(null, default);
            Assert.Null(await new FileTaskFailureBatchStateStore(config).ReadAsync(default));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CutoffSummaryIncludesPendingAccountsAndDailyMarkerSurvivesRestart()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CookieCheck:ServerChanSendKey"] = "SCT123synthetic",
                }
            )
            .Build();
        var handler = new CaptureHandler("{\"code\":0}");
        using var factory = new Factory(handler);
        var now = new DateTimeOffset(2026, 10, 6, 23, 55, 0, TimeSpan.FromHours(8));
        var day = new DateOnly(2026, 10, 6);
        var summary = new TaskFailureSummary(
            now.AddHours(-10),
            now,
            [new("直播粉丝牌", ["***1234"], 2, ["***1234"], false)],
            day,
            true
        );
        Assert.True(
            await new ServerChanTaskFailureNotifier(factory, config).SendAsync(summary, default)
        );
        var body = Uri.UnescapeDataString(handler.Body!.Replace("+", " "));
        Assert.Contains("2026-10-06", body);
        Assert.Contains("最终汇总时间", body);
        Assert.Contains("未完成账号：***1234", body);
        Assert.Contains("今日失败 2 次", body);
        Assert.DoesNotContain("SCT123synthetic", body);
        var directory = Directory.CreateTempSubdirectory("daily-summary-state-");
        try
        {
            config["TaskFailureNotification:StateFile"] = Path.Combine(
                directory.FullName,
                "state.json"
            );
            var state = new TaskFailureBatchState(
                now,
                now,
                now,
                [],
                [new(day, now, now, now, now, true, [])]
            );
            await new FileTaskFailureBatchStateStore(config).WriteAsync(state, default);
            var restored = await new FileTaskFailureBatchStateStore(config).ReadAsync(default);
            Assert.Equal(now, Assert.Single(restored!.Days!).NotificationAttemptUtc);
            Assert.Equal(day, Assert.Single(restored.Days!).Day);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
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
