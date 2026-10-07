using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

[Collection("Notification configuration diagnostics")]
public class WorkWeiXinAppVerificationTests
{
    [Theory]
    [InlineData("{\"errcode\":40013,\"errmsg\":\"synthetic-secret\"}", 200, "40013")]
    [InlineData("{\"errcode\":60020}", 200, "60020")]
    [InlineData("{\"errcode\":0}", 200, "访问令牌")]
    [InlineData("{\"errcode\":0,\"access_token\":\"\"}", 200, "访问令牌")]
    [InlineData("{\"errcode\":0,\"access_token\":\"  \"}", 200, "访问令牌")]
    [InlineData("{\"errcode\":0,\"access_token\":123}", 200, "访问令牌")]
    [InlineData("{\"errcode\":0,\"access_token\":null}", 200, "访问令牌")]
    [InlineData("{\"errcode\":\"0\",\"access_token\":\"synthetic-token\"}", 200, "确认结果")]
    [InlineData("{\"access_token\":\"synthetic-token\"}", 200, "确认结果")]
    [InlineData("[]", 200, "确认结果")]
    [InlineData("not-json synthetic-secret", 200, "确认结果")]
    [InlineData("{}", 503, "503")]
    [InlineData("{\"errcode\":0,\"access_token\":\"synthetic-token\"}", 401, "401")]
    public async Task InvalidToken_StopsBeforeSendingAndReportsOriginalStage(
        string response,
        int status,
        string expected
    )
    {
        var receiver = new Receiver { TokenBody = response, TokenStatus = (HttpStatusCode)status };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        var error = await Assert.ThrowsAnyAsync<Exception>(() => sink.Send("message"));
        Assert.Contains("获取令牌", error.Message);
        Assert.Contains(expected, error.Message);
        AssertSanitized(error);
        Assert.Equal(HttpMethod.Get, Assert.Single(receiver.Requests).Method);
    }

    [Theory]
    [InlineData("{\"errcode\":40014,\"errmsg\":\"synthetic-token\"}", 200, "40014")]
    [InlineData("{\"errcode\":81013}", 200, "81013")]
    [InlineData("{\"errcode\":\"0\"}", 200, "确认结果")]
    [InlineData("{}", 200, "确认结果")]
    [InlineData("[]", 200, "确认结果")]
    [InlineData("not-json synthetic-secret", 200, "确认结果")]
    [InlineData("synthetic-token", 503, "503")]
    public async Task RejectedMessage_ReportsSendingStageWithoutRetry(
        string response,
        int status,
        string expected
    )
    {
        var receiver = new Receiver
        {
            MessageBody = response,
            MessageStatus = (HttpStatusCode)status,
        };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        var error = await Assert.ThrowsAnyAsync<Exception>(() => sink.Send("message"));
        Assert.Contains("发送通知", error.Message);
        Assert.Contains(expected, error.Message);
        AssertSanitized(error);
        Assert.Equal(2, receiver.Requests.Count);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task TransportFailure_IsSanitizedAndDoesNotRetry(int failAt, bool canceled)
    {
        var receiver = new Receiver { FailAt = failAt, Canceled = canceled };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        var error = await Assert.ThrowsAnyAsync<Exception>(() => sink.Send("message"));
        Assert.Contains(failAt == 1 ? "获取令牌" : "发送通知", error.Message);
        AssertSanitized(error);
        Assert.Equal(failAt, receiver.Requests.Count);
    }

    [Fact]
    public async Task AcceptedMessage_PreservesRecipientFieldsAndEncodesCredentials()
    {
        var receiver = new Receiver
        {
            TokenBody = "{\"errcode\":0,\"access_token\":\"synthetic-token&plus+=中\"}",
        };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http);
        string message = "first" + Environment.NewLine + "second 😀";
        await sink.Send(message, "ignored legacy title");
        Assert.Equal(2, receiver.Requests.Count);
        Assert.Equal(
            "?corpid=synthetic-corp%26%2B%E4%B8%AD&corpsecret=synthetic-secret%26%2B%E4%B8%AD",
            receiver.Requests[0].Uri.Query
        );
        Assert.Equal(
            "?access_token=synthetic-token%26plus%2B%3D%E4%B8%AD",
            receiver.Requests[1].Uri.Query
        );
        Assert.Equal(HttpMethod.Post, receiver.Requests[1].Method);
        using var document = JsonDocument.Parse(receiver.Requests[1].Body!);
        var body = document.RootElement;
        Assert.Equal("user-a|user-b", body.GetProperty("touser").GetString());
        Assert.Equal("1|2", body.GetProperty("toparty").GetString());
        Assert.Equal("3|4", body.GetProperty("totag").GetString());
        Assert.Equal("synthetic-agent", body.GetProperty("agentid").GetString());
        Assert.Equal("text", body.GetProperty("msgtype").GetString());
        Assert.Equal(
            "first\nsecond 😀",
            body.GetProperty("text").GetProperty("content").GetString()
        );
        Assert.Equal("application/json", receiver.Requests[1].ContentType);
    }

    [Theory]
    [InlineData("", "agent", "secret")]
    [InlineData(" ", "agent", "secret")]
    [InlineData("corp", "", "secret")]
    [InlineData("corp", " ", "secret")]
    [InlineData("corp", "agent", "")]
    [InlineData("corp", "agent", " ")]
    public async Task DisabledConfiguration_DoesNotSend(string corp, string agent, string secret)
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new VerifiedWorkWeiXinAppBatchedSink(
            corp,
            agent,
            secret,
            httpClient: http
        );
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("Disabled configuration probe");
        await sink.FlushAllAsync();
        Assert.Empty(receiver.Requests);
    }

    [Theory]
    [InlineData("WorkWeiXinAppBatched")]
    [InlineData("workweixinappbatched")]
    public void LegacyConfiguration_BindsAllArgumentsWithoutChangingStoredSettings(string name)
    {
        var original = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Serilog:Using:2"] = "Ray.Serilog.Sinks.WorkWeiXinAppBatched",
                    ["Serilog:WriteTo:0:Name"] = name,
                    ["Serilog:WriteTo:0:Args:corpId"] = "",
                    ["Serilog:WriteTo:0:Args:agentId"] = "",
                    ["Serilog:WriteTo:0:Args:secret"] = "",
                    ["Serilog:WriteTo:0:Args:toUser"] = "user-a",
                    ["Serilog:WriteTo:0:Args:toParty"] = "1",
                    ["Serilog:WriteTo:0:Args:toTag"] = "2",
                    ["Serilog:WriteTo:0:Args:sendBatchesAsOneMessages"] = "false",
                    ["Serilog:WriteTo:0:Args:batchSizeLimit"] = "100",
                    ["Serilog:WriteTo:0:Args:outputTemplate"] = "{Message:lj}",
                    ["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"] = "Warning",
                    ["Serilog:WriteTo:1:Name"] = "WorkWeiXinBatched",
                    ["Serilog:WriteTo:1:Args:webHookUrl"] = "",
                    ["Serilog:WriteTo:2:Name"] = "Console",
                }
            )
            .Build();
        var effective = original.WithWorkWeiXinMessageChunking();
        Assert.Equal(name, original["Serilog:WriteTo:0:Name"]);
        Assert.Equal("VerifiedWorkWeiXinAppBatched", effective["Serilog:WriteTo:0:Name"]);
        Assert.Equal("ChunkedWorkWeiXinBatched", effective["Serilog:WriteTo:1:Name"]);
        Assert.Equal("Console", effective["Serilog:WriteTo:2:Name"]);
        Assert.Equal("user-a", effective["Serilog:WriteTo:0:Args:toUser"]);
        Assert.Equal("Ray.Serilog.Sinks.WorkWeiXinAppBatched", effective["Serilog:Using:2"]);
        Assert.Equal(
            typeof(VerifiedWorkWeiXinAppBatchedSink).Assembly.GetName().Name,
            effective["Serilog:Using:3"]
        );
        using var errors = new StringWriter();
        SelfLog.Enable(errors);
        try
        {
            using var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(effective)
                .CreateLogger();
            object aggregate = typeof(global::Serilog.Core.Logger)
                .GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(logger)!;
            var sinks = (global::Serilog.Core.ILogEventSink[])
                aggregate
                    .GetType()
                    .GetField("_sinks", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(aggregate)!;
            Assert.Contains(
                sinks,
                s =>
                    s is VerifiedWorkWeiXinAppBatchedSink
                    || s.GetType()
                        .GetField("_receiver", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(s) is VerifiedWorkWeiXinAppBatchedSink
            );
            logger.Warning("Disabled configuration probe");
            Assert.Equal("", errors.ToString());
        }
        finally
        {
            SelfLog.Disable();
        }
    }

    [Fact]
    public async Task LoggerFlush_PreservesGroupingAndFlushesOnDispose()
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
        await sink.FlushAsync("first", "title");
        Assert.Equal(2, receiver.Requests.Count);
        Assert.Contains("first-group", receiver.Requests[1].Body);
        logger.Dispose();
        Assert.Equal(4, receiver.Requests.Count);
        Assert.Contains("second-group", receiver.Requests[3].Body);
        Assert.False(receiver.Disposed);
    }

    [Fact]
    public async Task MinimumLevel_PreservesNotificationFiltering()
    {
        var receiver = new Receiver();
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(http, LogEventLevel.Warning);
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("filtered-message");
        logger.Warning("included-message");
        await sink.FlushAllAsync();
        Assert.Equal(2, receiver.Requests.Count);
        Assert.Contains("included-message", receiver.Requests[1].Body);
        Assert.DoesNotContain("filtered-message", receiver.Requests[1].Body!);
    }

    private static void AssertSanitized(Exception error)
    {
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.DoesNotContain("synthetic-token", error.ToString());
        Assert.DoesNotContain("synthetic-corp", error.ToString());
        Assert.DoesNotContain("qyapi.weixin.qq.com", error.ToString());
    }

    private sealed class ProbeSink(HttpClient http, LogEventLevel level = LogEventLevel.Verbose)
        : VerifiedWorkWeiXinAppBatchedSink(
            "synthetic-corp&+中",
            "synthetic-agent",
            "synthetic-secret&+中",
            "user-a|user-b",
            "1|2",
            "3|4",
            minimumLogEventLevel: level,
            httpClient: http
        )
    {
        public Task Send(string message, string title = "title") =>
            PushMessageAsync(message, title);
    }

    private sealed record Captured(HttpMethod Method, Uri Uri, string? Body, string? ContentType);

    private sealed class Receiver : HttpMessageHandler
    {
        public List<Captured> Requests { get; } = [];
        public string TokenBody { get; init; } =
            "{\"errcode\":0,\"access_token\":\"synthetic-token\"}";
        public HttpStatusCode TokenStatus { get; init; } = HttpStatusCode.OK;
        public string MessageBody { get; init; } = "{\"errcode\":0}";
        public HttpStatusCode MessageStatus { get; init; } = HttpStatusCode.OK;
        public int FailAt { get; init; }
        public bool Canceled { get; init; }
        public bool Disposed { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(
                new Captured(
                    request.Method,
                    request.RequestUri!,
                    request.Content is null
                        ? null
                        : await request.Content.ReadAsStringAsync(cancellationToken),
                    request.Content?.Headers.ContentType?.MediaType
                )
            );
            if (Requests.Count == FailAt)
            {
                if (Canceled)
                    throw new OperationCanceledException("synthetic-secret synthetic-token");
                throw new HttpRequestException(
                    "https://qyapi.weixin.qq.com/?synthetic-secret synthetic-token"
                );
            }
            return new HttpResponseMessage(
                request.Method == HttpMethod.Get ? TokenStatus : MessageStatus
            )
            {
                Content = new StringContent(
                    request.Method == HttpMethod.Get ? TokenBody : MessageBody
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
