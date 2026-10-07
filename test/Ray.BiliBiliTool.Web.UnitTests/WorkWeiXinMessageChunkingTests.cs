using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
using Serilog;
using Serilog.Debugging;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

[Collection("Notification configuration diagnostics")]
public class WorkWeiXinMessageChunkingTests
{
    [Theory]
    [InlineData(WorkWeiXinMsgType.markdown, "x", 4200)]
    [InlineData(WorkWeiXinMsgType.markdown, "中", 2000)]
    [InlineData(WorkWeiXinMsgType.markdown, "😀", 1200)]
    [InlineData(WorkWeiXinMsgType.markdown, "中\n", 1600)]
    [InlineData(WorkWeiXinMsgType.text, "x", 2500)]
    [InlineData(WorkWeiXinMsgType.text, "中", 1000)]
    [InlineData(WorkWeiXinMsgType.text, "😀", 600)]
    [InlineData(WorkWeiXinMsgType.markdown, "x", 32)]
    [InlineData(WorkWeiXinMsgType.text, "中", 32)]
    public async Task WireContent_FitsUtf8LimitAndRetainsCompleteMessage(
        WorkWeiXinMsgType type,
        string unit,
        int count
    )
    {
        var receiver = new Receiver(type);
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(type, http);
        string message = string.Concat(Enumerable.Repeat(unit, count));
        await sink.Send(message, "任务结果");
        Assert.Equal(
            "## 任务结果\r\n\r\n" + message.Replace("\n", "\r\n"),
            string.Concat(receiver.Contents)
        );
        Assert.All(
            receiver.Contents,
            part =>
            {
                Assert.InRange(Encoding.UTF8.GetByteCount(part), 1, receiver.Limit);
                Assert.False(char.IsHighSurrogate(part[^1]));
                Assert.False(char.IsLowSurrogate(part[0]));
            }
        );
        Assert.Equal(receiver.Contents.Count - 1, sink.Delays.Count);
        Assert.All(
            sink.Delays,
            delay => Assert.True(delay > TimeSpan.Zero && delay <= TimeSpan.FromMilliseconds(3100))
        );
        Assert.All(
            receiver.Urls,
            url => Assert.Equal("https://example.invalid/wecom?key=synthetic-key", url)
        );
    }

    [Theory]
    [InlineData(2047, 2048, 1)]
    [InlineData(2048, 2048, 1)]
    [InlineData(2049, 2048, 2)]
    [InlineData(4095, 4096, 1)]
    [InlineData(4096, 4096, 1)]
    [InlineData(4097, 4096, 2)]
    public void Split_RespectsExactAsciiBoundary(int length, int limit, int count)
    {
        string message = new('a', length);
        string[] parts = ChunkedWorkWeiXinBatchedSink.SplitMessage(message, limit).ToArray();
        Assert.Equal(count, parts.Length);
        Assert.Equal(message, string.Concat(parts));
    }

    [Theory]
    [InlineData("中", 1365, 4096, 1)]
    [InlineData("中", 1366, 4096, 2)]
    [InlineData("😀", 1024, 4096, 1)]
    [InlineData("😀", 1025, 4096, 2)]
    [InlineData("中", 682, 2048, 1)]
    [InlineData("中", 683, 2048, 2)]
    [InlineData("😀", 512, 2048, 1)]
    [InlineData("😀", 513, 2048, 2)]
    public void Split_RespectsUnicodeByteBoundary(string unit, int count, int limit, int partsCount)
    {
        string message = string.Concat(Enumerable.Repeat(unit, count));
        var parts = ChunkedWorkWeiXinBatchedSink.SplitMessage(message, limit).ToArray();
        Assert.Equal(partsCount, parts.Length);
        Assert.Equal(message, string.Concat(parts));
        Assert.All(parts, part => Assert.InRange(Encoding.UTF8.GetByteCount(part), 1, limit));
    }

    [Fact]
    public void Split_KeepsNewlineAndSurrogateBoundaries()
    {
        string message = "1234567\r\n中😀";
        var parts = ChunkedWorkWeiXinBatchedSink.SplitMessage(message, 8).ToArray();
        Assert.Equal("1234567", parts[0]);
        Assert.Equal(message, string.Concat(parts));
        Assert.DoesNotContain(parts, part => part.EndsWith('\r'));
        Assert.All(
            parts,
            part =>
            {
                Assert.InRange(Encoding.UTF8.GetByteCount(part), 1, 8);
                Assert.False(char.IsHighSurrogate(part[^1]));
                Assert.False(char.IsLowSurrogate(part[0]));
            }
        );
    }

    [Fact]
    public async Task Formatting_HandlesMixedNewlinesAndLongTitleBeforeSplitting()
    {
        var receiver = new Receiver(WorkWeiXinMsgType.markdown);
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(WorkWeiXinMsgType.markdown, http);
        string title = new('中', 2000);
        await sink.Send("first\r\nsecond\nthird\rfourth", title);
        Assert.Equal(
            "## " + title + "\r\n\r\nfirst\r\nsecond\r\nthird\r\nfourth",
            string.Concat(receiver.Contents)
        );
        Assert.True(receiver.Contents.Count > 1);
    }

    [Theory]
    [InlineData("", "## Work Weixin Notification\r\n\r\n")]
    [InlineData(" ", "## Work Weixin Notification\r\n\r\n")]
    public async Task Formatting_PreservesLegacyDefaultTitle(string title, string expected)
    {
        var receiver = new Receiver(WorkWeiXinMsgType.text);
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(WorkWeiXinMsgType.text, http);
        await sink.Send("", title);
        Assert.Equal(expected, Assert.Single(receiver.Contents));
    }

    [Theory]
    [InlineData("{\"errcode\":40058,\"errmsg\":\"synthetic-key\"}", 200, "40058")]
    [InlineData("{\"errcode\":45009}", 200, "45009")]
    [InlineData("{\"errcode\":\"0\"}", 200, "确认结果")]
    [InlineData("{}", 200, "确认结果")]
    [InlineData("[]", 200, "确认结果")]
    [InlineData("not-json synthetic-key", 200, "确认结果")]
    [InlineData("synthetic-key", 503, "503")]
    public async Task RejectedPart_StopsWithoutRetryOrCredentialDisclosure(
        string body,
        int status,
        string expected
    )
    {
        var receiver = new Receiver(WorkWeiXinMsgType.markdown)
        {
            FailAt = 2,
            FailureBody = body,
            FailureStatus = (HttpStatusCode)status,
        };
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(WorkWeiXinMsgType.markdown, http);
        var error = await Assert.ThrowsAnyAsync<Exception>(() =>
            sink.Send(new string('中', 10000), "任务")
        );
        Assert.Contains(expected, error.Message);
        Assert.DoesNotContain("synthetic-key", error.ToString());
        Assert.Equal(2, receiver.Contents.Count);
    }

    [Fact]
    public async Task LoggerFlush_PreservesTaskGroupingAndFlushesRemainingGroupOnDispose()
    {
        var receiver = new Receiver(WorkWeiXinMsgType.markdown);
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(WorkWeiXinMsgType.markdown, http);
        var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger
            .ForContext(global::Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey, "first")
            .Information("{Content}", new string('中', 2000));
        logger
            .ForContext(global::Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey, "second")
            .Information("second-group-message");
        await sink.FlushAsync("first", "first-title");
        Assert.DoesNotContain(receiver.Contents, part => part.Contains("second-group-message"));
        int count = receiver.Contents.Count;
        logger.Dispose();
        Assert.Equal(count + 1, receiver.Contents.Count);
        Assert.Contains("second-group-message", receiver.Contents[^1]);
        Assert.False(receiver.Disposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task DisabledConfiguration_DoesNotSend(string url)
    {
        var receiver = new Receiver(WorkWeiXinMsgType.text);
        using var http = new HttpClient(receiver);
        using var sink = new ChunkedWorkWeiXinBatchedSink(url, httpClient: http);
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("Disabled message");
        await sink.FlushAllAsync();
        Assert.Empty(receiver.Contents);
    }

    [Theory]
    [InlineData("WorkWeiXinBatched", "text")]
    [InlineData("workweixinbatched", "markdown")]
    public void LegacySettings_BindToChunkedSinkWithoutEditingStoredConfiguration(
        string name,
        string type
    )
    {
        var original = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Serilog:Using:2"] = "Ray.Serilog.Sinks.WorkWeiXinBatched",
                    ["Serilog:WriteTo:0:Name"] = name,
                    ["Serilog:WriteTo:0:Args:webHookUrl"] = "",
                    ["Serilog:WriteTo:0:Args:msgType"] = type,
                    ["Serilog:WriteTo:0:Args:outputTemplate"] = "{Message:lj}",
                    ["Serilog:WriteTo:0:Args:sendBatchesAsOneMessages"] = "false",
                    ["Serilog:WriteTo:0:Args:batchSizeLimit"] = "100",
                    ["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"] = "Warning",
                    ["Serilog:WriteTo:1:Name"] = "Console",
                }
            )
            .Build();
        IConfiguration effective = original
            .WithCompatibleHttpNotifications()
            .WithTelegramMessageChunking()
            .WithWorkWeiXinMessageChunking();
        Assert.Equal(name, original["Serilog:WriteTo:0:Name"]);
        Assert.Equal("ChunkedWorkWeiXinBatched", effective["Serilog:WriteTo:0:Name"]);
        Assert.Equal(type, effective["Serilog:WriteTo:0:Args:msgType"]);
        Assert.Equal("Console", effective["Serilog:WriteTo:1:Name"]);
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
                sink =>
                    sink is ChunkedWorkWeiXinBatchedSink
                    || sink.GetType()
                        .GetField("_receiver", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(sink) is ChunkedWorkWeiXinBatchedSink
            );
            logger.Warning("Disabled compatibility probe");
            Assert.Equal("", errors.ToString());
        }
        finally
        {
            SelfLog.Disable();
        }
    }

    [Fact]
    public async Task MinimumLevel_KeepsConfiguredNotificationFiltering()
    {
        var receiver = new Receiver(WorkWeiXinMsgType.text);
        using var http = new HttpClient(receiver);
        using var sink = new ProbeSink(
            WorkWeiXinMsgType.text,
            http,
            global::Serilog.Events.LogEventLevel.Warning
        );
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();
        logger.Information("filtered-information");
        logger.Warning("included-warning");
        await sink.FlushAllAsync("Filter");
        string content = Assert.Single(receiver.Contents);
        Assert.DoesNotContain("filtered-information", content);
        Assert.Contains("included-warning", content);
    }

    private sealed class ProbeSink(
        WorkWeiXinMsgType type,
        HttpClient http,
        global::Serilog.Events.LogEventLevel minimum = global::Serilog.Events.LogEventLevel.Verbose
    )
        : ChunkedWorkWeiXinBatchedSink(
            "https://example.invalid/wecom?key=synthetic-key",
            type,
            minimumLogEventLevel: minimum,
            httpClient: http
        )
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task Send(string message, string title) => PushMessageAsync(message, title);

        protected override Task DelayAsync(TimeSpan delay)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class Receiver(WorkWeiXinMsgType type) : HttpMessageHandler
    {
        public int Limit => type == WorkWeiXinMsgType.markdown ? 4096 : 2048;
        public List<string> Contents { get; } = [];
        public List<string> Urls { get; } = [];
        public bool Disposed { get; private set; }
        public int FailAt { get; init; } = int.MaxValue;
        public string FailureBody { get; init; } = "{}";
        public HttpStatusCode FailureStatus { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            using var body = JsonDocument.Parse(
                await request.Content.ReadAsStringAsync(cancellationToken)
            );
            string key = type == WorkWeiXinMsgType.markdown ? "markdown" : "text";
            Assert.Equal(key, body.RootElement.GetProperty("msgtype").GetString());
            string content = body.RootElement.GetProperty(key).GetProperty("content").GetString()!;
            Contents.Add(content);
            Urls.Add(request.RequestUri!.ToString());
            Assert.InRange(Encoding.UTF8.GetByteCount(content), 1, Limit);
            return new HttpResponseMessage(
                Contents.Count == FailAt ? FailureStatus : HttpStatusCode.OK
            )
            {
                Content = new StringContent(
                    Contents.Count == FailAt ? FailureBody : "{\"errcode\":0}"
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
