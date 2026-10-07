using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Serilog;
using Serilog.Debugging;

namespace Ray.BiliBiliTool.Infrastructure.UnitTests;

[Collection("Notification configuration diagnostics")]
public class HttpNotificationTests
{
    [Fact]
    public async Task Gotify_SendsObjectExtrasAndApplicationTokenHeader()
    {
        var captured = new CaptureHandler();
        using var http = new HttpClient(captured);
        using var sender = new NotificationPushService(
            "https://example.invalid/gotify/",
            "test-token",
            null,
            "",
            httpClient: http
        );
        using var response = await sender.PushMessageAsync(
            "quoted \"message\"\n第二行",
            "Test title"
        );
        Assert.Equal("https://example.invalid/gotify/message", captured.Url);
        Assert.Equal("test-token", captured.Headers["X-Gotify-Key"]);
        using var body = JsonDocument.Parse(captured.Body);
        Assert.Equal("Test title", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "quoted \"message\"\n第二行",
            body.RootElement.GetProperty("message").GetString()
        );
        Assert.Equal(
            "text/markdown",
            body.RootElement.GetProperty("extras")
                .GetProperty("client::display")
                .GetProperty("contentType")
                .GetString()
        );
        Assert.False(body.RootElement.GetProperty("extras").TryGetProperty("extras", out _));
    }

    [Fact]
    public async Task CustomApi_BindsHeadersAndEscapesTemplateMessage()
    {
        var captured = new CaptureHandler();
        using var http = new HttpClient(captured);
        using var sender = new NotificationPushService(
            "https://example.invalid/push",
            null,
            "{\"text\":#msg#}",
            "#msg#",
            new() { ["Authorization"] = "Bearer test-value", ["X-Api-Key"] = "test-key" },
            http
        );
        using var response = await sender.PushMessageAsync("quote\"\n中文\\", "title");
        Assert.Equal("Bearer test-value", captured.Headers["Authorization"]);
        Assert.Equal("test-key", captured.Headers["X-Api-Key"]);
        using var body = JsonDocument.Parse(captured.Body);
        Assert.Equal("quote\"\n中文\\", body.RootElement.GetProperty("text").GetString());
        Assert.Equal("application/json", captured.MediaType);
    }

    [Fact]
    public async Task BatchedSink_FlushesAsOneRequest()
    {
        var captured = new CaptureHandler();
        using var http = new HttpClient(captured);
        using var sink = new HttpNotificationBatchedSink(
            "https://example.invalid",
            "test-token",
            null,
            "",
            httpClient: http
        );
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("First");
        logger.Information("Second");
        await sink.FlushAllAsync("Batch");
        Assert.Equal(1, captured.Calls);
        using var body = JsonDocument.Parse(captured.Body);
        Assert.Contains("First", body.RootElement.GetProperty("message").GetString());
        Assert.Contains("Second", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task FailedNotification_ReportsHttpStatusWithoutResponseSecrets()
    {
        var captured = new CaptureHandler { Status = HttpStatusCode.Unauthorized };
        using var http = new HttpClient(captured);
        using var sink = new HttpNotificationBatchedSink(
            "https://example.invalid",
            "test-token",
            null,
            "",
            httpClient: http
        );
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("Message");
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => sink.FlushAllAsync());
        Assert.Contains("401", error.Message);
        Assert.DoesNotContain("test-token", error.Message);
    }

    [Theory]
    [InlineData("GotifyBatched", "CompatibleGotifyBatched")]
    [InlineData("OtherApiBatched", "HeaderOtherApiBatched")]
    public void Configuration_UsesCompatibleSinkWithoutChangingStoredSettings(
        string original,
        string compatible
    )
    {
        var values = new Dictionary<string, string?>
        {
            ["Serilog:Using:3"] = "Serilog",
            ["Serilog:WriteTo:8:Name"] = original,
            ["Serilog:WriteTo:8:Args:host"] = "",
            ["Serilog:WriteTo:8:Args:token"] = "",
            ["Serilog:WriteTo:8:Args:api"] = "",
            ["Serilog:WriteTo:8:Args:bodyJsonTemplate"] = "{\"text\":#msg#}",
            ["Serilog:WriteTo:8:Args:placeholder"] = "#msg#",
            ["Serilog:WriteTo:8:Args:headers:X-Api-Key"] = "test-value",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var effective = configuration.WithCompatibleHttpNotifications();
        Assert.Equal(original, configuration["Serilog:WriteTo:8:Name"]);
        Assert.Equal(compatible, effective["Serilog:WriteTo:8:Name"]);
        Assert.Equal("test-value", effective["Serilog:WriteTo:8:Args:headers:X-Api-Key"]);
        Assert.Equal(
            typeof(HttpNotificationBatchedSink).Assembly.GetName().Name,
            effective["Serilog:Using:4"]
        );
        using var errors = new StringWriter();
        SelfLog.Enable(errors);
        try
        {
            using var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(effective)
                .CreateLogger();
            logger.Information("Disabled sink");
            Assert.Equal("", errors.ToString());
        }
        finally
        {
            SelfLog.Disable();
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public int Calls { get; private set; }
        public string? Url { get; private set; }
        public string Body { get; private set; } = "";
        public string? MediaType { get; private set; }
        public Dictionary<string, string> Headers { get; private set; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Calls++;
            Url = request.RequestUri?.ToString();
            Headers = request.Headers.ToDictionary(
                header => header.Key,
                header => string.Join(",", header.Value)
            );
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            MediaType = request.Content.Headers.ContentType?.MediaType;
            return new(Status) { Content = new StringContent("test-token") };
        }
    }
}
