using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

[Collection("Notification configuration diagnostics")]
public class TelegramTransportTests
{
    [Fact]
    public void OwnedClient_UsesThirtySecondTimeout()
    {
        using var sink = new ChunkedTelegramBatchedSink("123456:synthetic-token", "synthetic-chat");
        Assert.Equal(TimeSpan.FromSeconds(30), Client(sink).Timeout);
    }

    [Fact]
    public async Task SlowAcceptedResponse_CompletesBeyondOldFiveSecondLimit()
    {
        var receiver = new Receiver { Delay = TimeSpan.FromMilliseconds(5500) };
        using var defaults = new ChunkedTelegramBatchedSink(
            "123456:synthetic-token",
            "synthetic-chat"
        );
        using var http = new HttpClient(receiver) { Timeout = Client(defaults).Timeout };
        using var sink = new ProbeSink(http);
        await sink.Send("message", "title");
        Assert.Single(receiver.Messages);
    }

    [Theory]
    [InlineData(
        "{\"ok\":false,\"error_code\":400,\"description\":\"synthetic-token\"}",
        200,
        "400"
    )]
    [InlineData("{\"ok\":false,\"error_code\":429}", 200, "429")]
    [InlineData("{\"ok\":false}", 200, "拒绝")]
    [InlineData("{}", 200, "确认结果")]
    [InlineData("{\"ok\":\"true\"}", 200, "确认结果")]
    [InlineData("{\"ok\":null}", 200, "确认结果")]
    [InlineData("[]", 200, "确认结果")]
    [InlineData("not-json synthetic-token", 200, "确认结果")]
    [InlineData("synthetic-token", 503, "503")]
    [InlineData("synthetic-token", 429, "429")]
    public async Task RejectedPart_StopsWithoutRetryOrCredentialDisclosure(
        string body,
        int status,
        string expected
    )
    {
        var receiver = new Receiver
        {
            FailureBody = body,
            FailureStatus = (HttpStatusCode)status,
            FailAt = 2,
        };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        var error = await Assert.ThrowsAnyAsync<Exception>(() =>
            sink.Send(new string('中', 10000), "title")
        );
        Assert.Contains(expected, error.Message);
        Assert.DoesNotContain("synthetic-token", error.ToString());
        Assert.DoesNotContain("synthetic-chat", error.ToString());
        Assert.Equal(2, receiver.Messages.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportFailure_IsSanitizedAndDoesNotRetry(bool canceled)
    {
        var receiver = new Receiver { ThrowAt = 1, Canceled = canceled };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        var error = await Assert.ThrowsAnyAsync<Exception>(() => sink.Send("message", "title"));
        Assert.DoesNotContain("synthetic-token", error.ToString());
        Assert.DoesNotContain("synthetic-chat", error.ToString());
        Assert.DoesNotContain("api.telegram.org", error.ToString());
        Assert.Single(receiver.Messages);
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("unicode")]
    [InlineData("emoji")]
    [InlineData("html")]
    [InlineData("short")]
    public async Task ActualWire_PreservesTitleFullMessageAndProtocolFields(string kind)
    {
        string message = kind switch
        {
            "ascii" => new string('x', 10000),
            "unicode" => new string('中', 6000),
            "emoji" => string.Concat(Enumerable.Repeat("😀", 2500)),
            "html" => string.Concat(Enumerable.Repeat("<tag>&\n", 1200)),
            _ => "short message",
        };
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        string title = "任务<&>";
        string prefix =
            "<b>"
            + WebUtility.HtmlEncode(title)
            + "</b>"
            + Environment.NewLine
            + Environment.NewLine;
        await sink.Send(message, title);
        Assert.Equal(
            message,
            string.Concat(
                receiver.Messages.Select(text => WebUtility.HtmlDecode(text[prefix.Length..]))
            )
        );
        Assert.All(
            receiver.Messages,
            text =>
            {
                Assert.StartsWith(prefix, text);
                string body = WebUtility.HtmlDecode(text[prefix.Length..]);
                Assert.InRange(
                    body.Length + title.Length + 2 * Environment.NewLine.Length,
                    1,
                    4096
                );
                Assert.False(char.IsHighSurrogate(body[^1]));
                Assert.False(char.IsLowSurrogate(body[0]));
                Assert.DoesNotContain("<tag>", text);
            }
        );
        Assert.All(
            receiver.Urls,
            uri =>
                Assert.Equal("https://api.telegram.org/bot123456:synthetic-token/sendMessage", uri)
        );
        Assert.All(receiver.ChatIds, chat => Assert.Equal("synthetic-chat", chat));
        Assert.All(receiver.ContentTypes, type => Assert.Equal("application/json", type));
    }

    [Fact]
    public async Task LongTitle_IsTrimmedAtSafeBoundaryAndStillSent()
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        await sink.Send("message", string.Concat(Enumerable.Repeat("😀", 200)));
        string text = WebUtility.HtmlDecode(Assert.Single(receiver.Messages));
        string title = text[3..text.IndexOf("</b>", StringComparison.Ordinal)];
        Assert.InRange(title.Length, 1, 256);
        Assert.False(char.IsHighSurrogate(title[^1]));
        Assert.EndsWith("message", text);
    }

    [Theory]
    [InlineData("example.invalid:8080", "http://example.invalid:8080/", "", "")]
    [InlineData("http://example.invalid:8080", "http://example.invalid:8080/", "", "")]
    [InlineData("user:pa:ss@example.invalid:8080", "http://example.invalid:8080/", "user", "pa:ss")]
    [InlineData(
        "http://user:pass@example.invalid:8080",
        "http://example.invalid:8080/",
        "user",
        "pass"
    )]
    [InlineData(
        "https://u%40ser:p%3Aa%40ss@example.invalid:8080",
        "https://example.invalid:8080/",
        "u@ser",
        "p:a@ss"
    )]
    [InlineData(
        "socks5://user:pass@example.invalid:1080",
        "socks5://example.invalid:1080/",
        "user",
        "pass"
    )]
    [InlineData("socks4://user@example.invalid:1080", "socks4://example.invalid:1080/", "user", "")]
    [InlineData(
        "socks4a://user@example.invalid:1080",
        "socks4a://example.invalid:1080/",
        "user",
        ""
    )]
    public void ProxyConfiguration_PreservesAddressAndAuthentication(
        string address,
        string expected,
        string user,
        string password
    )
    {
        using var sink = new ChunkedTelegramBatchedSink(
            "123456:synthetic-token",
            "synthetic-chat",
            address
        );
        var handler = (HttpClientHandler)
            typeof(HttpMessageInvoker)
                .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Client(sink))!;
        var proxy = (WebProxy)handler.Proxy!;
        Assert.True(handler.UseProxy);
        Assert.Equal(expected, proxy.Address!.AbsoluteUri);
        Assert.Equal("", proxy.Address.UserInfo);
        var credentials = proxy.Credentials?.GetCredential(proxy.Address, "Basic");
        Assert.Equal(user, credentials?.UserName ?? "");
        Assert.Equal(password, credentials?.Password ?? "");
        Assert.Equal(TimeSpan.FromSeconds(30), Client(sink).Timeout);
    }

    [Theory]
    [InlineData("ftp://user:synthetic-proxy-pass@example.invalid")]
    [InlineData("http://user:synthetic-proxy-pass@")]
    [InlineData("http://[synthetic-proxy-pass")]
    public void InvalidProxy_ReportsSafeError(string proxy)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new ChunkedTelegramBatchedSink("123456:synthetic-token", "synthetic-chat", proxy)
        );
        Assert.DoesNotContain("synthetic-proxy-pass", error.ToString());
        Assert.DoesNotContain("synthetic-token", error.ToString());
    }

    [Fact]
    public async Task ForwardProxy_UsesConnectAndConfiguredAuthentication()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var headers = new List<string>();
        var server = Task.Run(
            async () =>
            {
                for (int i = 0; i < 2; i++)
                {
                    using var client = await listener.AcceptTcpClientAsync(timeout.Token);
                    await using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                    var lines = new List<string>();
                    while (await reader.ReadLineAsync(timeout.Token) is { Length: > 0 } line)
                        lines.Add(line);
                    headers.Add(string.Join("\n", lines));
                    string response =
                        i == 0
                            ? "HTTP/1.1 407 Proxy Authentication Required\r\nProxy-Authenticate: Basic realm=\"synthetic\"\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                            : "HTTP/1.1 502 Bad Gateway\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response), timeout.Token);
                }
            },
            timeout.Token
        );
        using var sink = new NetworkProbeSink($"http://user:pa:ss@127.0.0.1:{port}");
        await Assert.ThrowsAsync<HttpRequestException>(() => sink.Send());
        await server;
        Assert.Equal(2, headers.Count);
        Assert.All(
            headers,
            value => Assert.Contains("CONNECT api.telegram.org:443 HTTP/1.1", value)
        );
        Assert.Contains(
            "Proxy-Authorization: Basic "
                + Convert.ToBase64String(Encoding.ASCII.GetBytes("user:pa:ss")),
            headers[1]
        );
    }

    [Theory]
    [InlineData("", "chat")]
    [InlineData(" ", "chat")]
    [InlineData("token", "")]
    [InlineData("token", " ")]
    public async Task DisabledConfiguration_DoesNotSend(string bot, string chat)
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new ChunkedTelegramBatchedSink(
            bot,
            chat,
            "invalid-proxy",
            httpClient: http
        );
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("Disabled message");
        await sink.FlushAllAsync();
        Assert.Empty(receiver.Messages);
    }

    [Fact]
    public async Task LoggerFlush_PreservesGroupsAndCallerClientOwnership()
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger
            .ForContext(global::Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey, "first")
            .Information("first-group");
        logger
            .ForContext(global::Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey, "second")
            .Information("second-group");
        await sink.FlushAsync("first", "first-title");
        Assert.Contains("first-title", Assert.Single(receiver.Messages));
        logger.Dispose();
        Assert.Equal(2, receiver.Messages.Count);
        Assert.Contains("second-group", receiver.Messages[1]);
        Assert.False(receiver.Disposed);
    }

    [Fact]
    public async Task MinimumLevel_KeepsConfiguredFiltering()
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http, LogEventLevel.Warning);
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("filtered-message");
        logger.Warning("included-message");
        await sink.FlushAllAsync();
        Assert.Contains("included-message", Assert.Single(receiver.Messages));
        Assert.DoesNotContain("filtered-message", receiver.Messages[0]);
    }

    [Fact]
    public async Task PhysicalSend_RespectsSpacingBetweenParts()
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new SpacingSink(http);
        await sink.Send(new string('x', 9000));
        Assert.Equal(3, receiver.SentAt.Count);
        for (int i = 1; i < receiver.SentAt.Count; i++)
            Assert.True(receiver.SentAt[i] - receiver.SentAt[i - 1] >= TimeSpan.FromSeconds(1));
    }

    private static HttpClient Client(ChunkedTelegramBatchedSink sink) =>
        (HttpClient)
            typeof(ChunkedTelegramBatchedSink)
                .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(sink)!;

    private sealed class ProbeSink(HttpClient http, LogEventLevel level = LogEventLevel.Verbose)
        : ChunkedTelegramBatchedSink(
            "123456:synthetic-token",
            "synthetic-chat",
            minimumLogEventLevel: level,
            httpClient: http
        )
    {
        public Task Send(string message, string title) => PushMessageAsync(message, title);

        protected override Task WaitForSendAsync() => Task.CompletedTask;
    }

    private sealed class SpacingSink(HttpClient http)
        : ChunkedTelegramBatchedSink("123456:synthetic-token", "synthetic-chat", httpClient: http)
    {
        public Task Send(string message) => PushMessageAsync(message, "title");
    }

    private sealed class NetworkProbeSink(string proxy)
        : ChunkedTelegramBatchedSink("123456:synthetic-token", "synthetic-chat", proxy)
    {
        public Task Send() => PushMessageAsync("message", "title");
    }

    private sealed class Receiver : HttpMessageHandler
    {
        public List<string> Messages { get; } = [];
        public List<string> Urls { get; } = [];
        public List<string> ChatIds { get; } = [];
        public List<string?> ContentTypes { get; } = [];
        public List<DateTimeOffset> SentAt { get; } = [];
        public string FailureBody { get; init; } = "{\"ok\":false}";
        public HttpStatusCode FailureStatus { get; init; } = HttpStatusCode.OK;
        public int FailAt { get; init; }
        public int ThrowAt { get; init; }
        public bool Canceled { get; init; }
        public TimeSpan Delay { get; init; }
        public bool Disposed { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken token
        )
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            using var document = JsonDocument.Parse(
                await request.Content!.ReadAsStringAsync(token)
            );
            var body = document.RootElement;
            Assert.Equal("HTML", body.GetProperty("parse_mode").GetString());
            Assert.True(body.GetProperty("disable_web_page_preview").GetBoolean());
            Messages.Add(body.GetProperty("text").GetString()!);
            Urls.Add(request.RequestUri!.AbsoluteUri);
            ChatIds.Add(body.GetProperty("chat_id").GetString()!);
            ContentTypes.Add(request.Content.Headers.ContentType?.MediaType);
            SentAt.Add(DateTimeOffset.UtcNow);
            if (Messages.Count == ThrowAt)
            {
                if (Canceled)
                    throw new OperationCanceledException("synthetic-token synthetic-chat");
                throw new HttpRequestException(
                    "https://api.telegram.org/botsynthetic-token synthetic-chat"
                );
            }
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, token);
            return new HttpResponseMessage(
                Messages.Count == FailAt ? FailureStatus : HttpStatusCode.OK
            )
            {
                Content = new StringContent(
                    Messages.Count == FailAt
                        ? FailureBody
                        : "{\"ok\":true,\"result\":{\"message_id\":1}}"
                ),
            };
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
