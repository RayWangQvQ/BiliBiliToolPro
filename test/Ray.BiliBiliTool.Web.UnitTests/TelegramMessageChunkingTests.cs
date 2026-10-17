using System.Net;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Serilog;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TelegramMessageChunkingTests
{
    [Theory]
    [InlineData(4095)]
    [InlineData(4096)]
    [InlineData(4097)]
    [InlineData(16000)]
    public async Task LongMessage_IsSentCompletelyAsValidHtmlParts(int length)
    {
        using var sink = new RecordingSink();
        var message = "<tag>&" + string.Concat(Enumerable.Repeat("😀", length / 2)) + "\n尾部";
        await sink.Send(message, "任务结果");
        Assert.Equal(
            message,
            string.Concat(sink.Parts.Select(part => WebUtility.HtmlDecode(part.Message)))
        );
        Assert.All(
            sink.Parts,
            part =>
            {
                var decoded = WebUtility.HtmlDecode(part.Message);
                Assert.InRange(
                    decoded.Length
                        + WebUtility.HtmlDecode(part.Title).Length
                        + 2 * Environment.NewLine.Length,
                    1,
                    4096
                );
                Assert.False(char.IsHighSurrogate(decoded[^1]));
                Assert.False(char.IsLowSurrogate(decoded[0]));
                Assert.DoesNotContain("<tag>", part.Message);
            }
        );
    }

    [Theory]
    [InlineData(4095, 1)]
    [InlineData(4096, 1)]
    [InlineData(4097, 2)]
    public void SplitMessage_HandlesExactLengthBoundary(int length, int count)
    {
        var message = new string('中', length);
        var parts = ChunkedTelegramBatchedSink.SplitMessage(message, 4096).ToArray();
        Assert.Equal(count, parts.Length);
        Assert.Equal(message, string.Concat(parts));
    }

    [Fact]
    public void SerilogConfiguration_InstantiatesChunkedSinkFromLegacySettings()
    {
        var original = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Serilog:WriteTo:0:Name"] = "TelegramBatched",
                    ["Serilog:WriteTo:0:Args:botToken"] = "",
                    ["Serilog:WriteTo:0:Args:chatId"] = "",
                    ["Serilog:Using:0"] = "Ray.Serilog.Sinks.TelegramBatched",
                }
            )
            .Build();
        using var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(original.WithTelegramMessageChunking())
            .CreateLogger();
        var aggregate = typeof(global::Serilog.Core.Logger)
            .GetField(
                "_sink",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            )!
            .GetValue(logger)!;
        var sinks = (global::Serilog.Core.ILogEventSink[])
            aggregate
                .GetType()
                .GetField(
                    "_sinks",
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                )!
                .GetValue(aggregate)!;
        Assert.Contains(sinks, sink => sink is ChunkedTelegramBatchedSink);
    }

    [Fact]
    public async Task LoggerFlush_UsesChunkingWithoutChangingBatchGrouping()
    {
        using var sink = new RecordingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger
            .ForContext(global::Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey, "job")
            .Information("{Text}", new string('中', 10000));
        logger
            .ForContext(global::Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey, "another-job")
            .Information("Another task");
        await sink.FlushAsync("job", "任务");
        Assert.True(sink.Parts.Count >= 3);
        Assert.DoesNotContain(
            sink.Parts,
            part => WebUtility.HtmlDecode(part.Message).Contains("Another task")
        );
        var firstBatchCount = sink.Parts.Count;
        await sink.FlushAsync("another-job", "另一项任务");
        Assert.Equal(firstBatchCount + 1, sink.Parts.Count);
        Assert.Contains("Another task", WebUtility.HtmlDecode(sink.Parts[^1].Message));
    }

    [Fact]
    public void ConfigurationOverlay_PreservesCredentialsAndStoredSinkName()
    {
        var original = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Serilog:WriteTo:0:Name"] = "TelegramBatched",
                    ["Serilog:WriteTo:0:Args:botToken"] = "test-token",
                    ["Serilog:Using:0"] = "Ray.Serilog.Sinks.TelegramBatched",
                }
            )
            .Build();
        var configured = original.WithTelegramMessageChunking();
        Assert.Equal("TelegramBatched", original["Serilog:WriteTo:0:Name"]);
        Assert.Equal("ChunkedTelegramBatched", configured["Serilog:WriteTo:0:Name"]);
        Assert.Equal("test-token", configured["Serilog:WriteTo:0:Args:botToken"]);
        Assert.Equal(
            typeof(ChunkedTelegramBatchedSink).Assembly.GetName().Name,
            configured["Serilog:Using:1"]
        );
    }

    private sealed class RecordingSink() : ChunkedTelegramBatchedSink("test-token", "test-chat")
    {
        public List<(string Message, string Title)> Parts { get; } = [];

        public Task Send(string message, string title) => PushMessageAsync(message, title);

        protected override Task WaitForSendAsync() => Task.CompletedTask;

        protected override Task SendPartAsync(string message, string title)
        {
            Parts.Add((message, title));
            return Task.CompletedTask;
        }
    }
}
