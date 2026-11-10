using System.Globalization;
using System.Net;
using Ray.Serilog.Sinks.TelegramBatched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public class ChunkedTelegramBatchedSink(
    string botToken,
    string chatId,
    string proxy = "",
    bool sendBatchesAsOneMessages = true,
    int batchSizeLimit = int.MaxValue,
    string outputTemplate = "{Message:lj}{NewLine}{Exception}",
    IFormatProvider? formatProvider = null,
    LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose
)
    : TelegramBatchedSink(
        botToken,
        chatId,
        proxy,
        sendBatchesAsOneMessages,
        batchSizeLimit,
        outputTemplate,
        formatProvider ?? CultureInfo.InvariantCulture,
        minimumLogEventLevel
    )
{
    private DateTimeOffset _lastSentAt;

    protected override async Task PushMessageAsync(string message, string title = "推送")
    {
        title = title.Length <= 256 ? title : SplitMessage(title, 256).First();
        var capacity = 4096 - title.Length - 2 * Environment.NewLine.Length;
        foreach (var part in SplitMessage(message, capacity))
        {
            await WaitForSendAsync();
            await SendPartAsync(WebUtility.HtmlEncode(part), WebUtility.HtmlEncode(title));
            _lastSentAt = DateTimeOffset.UtcNow;
        }
    }

    protected virtual async Task WaitForSendAsync()
    {
        var remaining = TimeSpan.FromMilliseconds(1100) - (DateTimeOffset.UtcNow - _lastSentAt);
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining);
    }

    protected virtual Task SendPartAsync(string message, string title) =>
        base.PushMessageAsync(message, title);

    public static IEnumerable<string> SplitMessage(string message, int capacity)
    {
        if (capacity < 2)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        if (message.Length == 0)
        {
            yield return "";
            yield break;
        }
        var offset = 0;
        while (offset < message.Length)
        {
            var length = Math.Min(capacity, message.Length - offset);
            if (offset + length < message.Length)
            {
                if (
                    char.IsHighSurrogate(message[offset + length - 1])
                    && char.IsLowSurrogate(message[offset + length])
                )
                    length--;
                var newline = message.LastIndexOf('\n', offset + length - 1, length);
                if (newline >= offset)
                    length = newline - offset + 1;
            }
            yield return message.Substring(offset, length);
            offset += length;
        }
    }
}

public static class ChunkedTelegramLoggerConfigurationExtensions
{
    public static LoggerConfiguration ChunkedTelegramBatched(
        this LoggerSinkConfiguration sink,
        string botToken,
        string chatId,
        string proxy = "",
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
    ) =>
        sink.Sink(
            new ChunkedTelegramBatchedSink(
                botToken,
                chatId,
                proxy,
                sendBatchesAsOneMessages,
                batchSizeLimit,
                outputTemplate,
                formatProvider,
                restrictedToMinimumLevel
            ),
            restrictedToMinimumLevel
        );
}
