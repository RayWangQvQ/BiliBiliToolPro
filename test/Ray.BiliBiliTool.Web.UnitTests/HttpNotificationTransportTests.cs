using System.Net;
using System.Reflection;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Serilog;

namespace Ray.BiliBiliTool.Infrastructure.UnitTests;

[Collection("Notification configuration diagnostics")]
public class HttpNotificationTransportTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task TransportFailure_DoesNotExposeCredentialsOrEndpoint(
        bool gotify,
        bool canceled
    )
    {
        var receiver = new Receiver { Fail = true, Canceled = canceled };
        using var http = new HttpClient(receiver);
        using var sink = Sink(http, gotify);
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("synthetic-message");
        var error = await Assert.ThrowsAnyAsync<Exception>(() => sink.FlushAllAsync("title"));
        if (canceled)
            Assert.True(
                Assert
                    .IsAssignableFrom<OperationCanceledException>(error)
                    .CancellationToken.IsCancellationRequested
            );
        else
            Assert.IsType<HttpRequestException>(error);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("synthetic-key", error.ToString());
        Assert.DoesNotContain("synthetic-token", error.ToString());
        Assert.DoesNotContain("example.invalid", error.ToString());
        Assert.Equal(1, receiver.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(401)]
    [InlineData(503)]
    public async Task HttpFailure_KeepsOptionalStatusCode(int status)
    {
        var receiver = new Receiver
        {
            Fail = true,
            Status = status == 0 ? null : (HttpStatusCode)status,
        };
        using var http = new HttpClient(receiver);
        using var sender = new NotificationPushService(
            "https://example.invalid",
            "synthetic-token",
            null,
            "",
            httpClient: http
        );
        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.PushMessageAsync("message", "title")
        );
        Assert.Equal(receiver.Status, error.StatusCode);
        Assert.Equal(HttpRequestError.NameResolutionError, error.HttpRequestError);
        Assert.Equal(1, receiver.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NextExplicitFlush_StillWorksAfterTransportFailure(bool gotify)
    {
        var receiver = new Receiver { Fail = true };
        using var http = new HttpClient(receiver);
        using var sink = Sink(http, gotify);
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("failed-message");
        await Assert.ThrowsAsync<HttpRequestException>(() => sink.FlushAllAsync("first"));
        Assert.Equal(1, receiver.Calls);
        receiver.Fail = false;
        logger.Information("recovered-message");
        await sink.FlushAllAsync("second");
        Assert.Equal(2, receiver.Calls);
        Assert.Contains("recovered-message", receiver.LastBody);
        Assert.DoesNotContain("failed-message", receiver.LastBody);
    }

    [Fact]
    public async Task SenderDisposal_PreservesInjectedClientAndDefaultTimeout()
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using (
            var sender = new NotificationPushService(
                "https://example.invalid",
                null,
                "{\"message\":#msg#}",
                "#msg#",
                httpClient: http
            )
        )
        using (await sender.PushMessageAsync("message")) { }
        using (await http.GetAsync("https://example.invalid")) { }
        Assert.Equal(2, receiver.Calls);
        using var owned = new NotificationPushService("", null, null, "");
        var client = (HttpClient)
            typeof(NotificationPushService)
                .GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(owned)!;
        Assert.Equal(TimeSpan.FromSeconds(100), client.Timeout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DirectSender_UsesTheSameSanitizedFailurePath(bool gotify)
    {
        var receiver = new Receiver { Fail = true };
        using var http = new HttpClient(receiver);
        using var sender = new NotificationPushService(
            "https://example.invalid",
            gotify ? "synthetic-token" : null,
            "{\"message\":#msg#}",
            "#msg#",
            httpClient: http
        );
        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.PushMessageAsync("message", "title")
        );
        Assert.DoesNotContain("synthetic-key", error.ToString());
        Assert.DoesNotContain("example.invalid", error.ToString());
        Assert.Null(error.InnerException);
        Assert.Equal(1, receiver.Calls);
    }

    private static HttpNotificationBatchedSink Sink(HttpClient http, bool gotify) =>
        new(
            "https://example.invalid",
            gotify ? "synthetic-token" : null,
            "{\"message\":#msg#}",
            "#msg#",
            new() { ["Authorization"] = "Bearer synthetic-key" },
            httpClient: http
        );

    private sealed class Receiver : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public bool Canceled { get; init; }
        public HttpStatusCode? Status { get; init; } = HttpStatusCode.BadGateway;
        public string LastBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Calls++;
            LastBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Fail)
            {
                const string privateDetails =
                    "https://example.invalid/?token=synthetic-key synthetic-token";
                if (Canceled)
                    throw new OperationCanceledException(
                        privateDetails,
                        new Exception(privateDetails),
                        new CancellationToken(true)
                    );
                throw new HttpRequestException(
                    HttpRequestError.NameResolutionError,
                    privateDetails,
                    new Exception(privateDetails),
                    Status
                );
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("accepted"),
            };
        }
    }
}
