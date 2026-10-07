using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public class ChunkedWorkWeiXinBatchedSink : WorkWeiXinBatchedSink
{
    private readonly string _webHookUrl;
    private readonly WorkWeiXinMsgType _msgType;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly bool _enabled;
    private long? _lastAttempt;

    public ChunkedWorkWeiXinBatchedSink(
        string webHookUrl,
        WorkWeiXinMsgType msgType = WorkWeiXinMsgType.text,
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose,
        HttpClient? httpClient = null
    )
        : base(
            webHookUrl,
            msgType,
            sendBatchesAsOneMessages,
            batchSizeLimit,
            outputTemplate,
            formatProvider ?? CultureInfo.InvariantCulture,
            minimumLogEventLevel
        )
    {
        _webHookUrl = webHookUrl;
        _msgType = msgType;
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
        _enabled = !string.IsNullOrWhiteSpace(webHookUrl);
    }

    public override void Emit(LogEvent logEvent)
    {
        if (_enabled)
            base.Emit(logEvent);
    }

    protected override async Task PushMessageAsync(string message, string title = "推送")
    {
        title = string.IsNullOrWhiteSpace(title) ? Constants.DefaultTitle : title;
        string content = $"## {title}\n\n{message}"
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\n", "\r\n");
        int limit = _msgType == WorkWeiXinMsgType.markdown ? 4096 : 2048;
        foreach (string part in SplitMessage(content, limit))
        {
            if (_lastAttempt is long previous)
            {
                TimeSpan remaining =
                    TimeSpan.FromMilliseconds(3100) - Stopwatch.GetElapsedTime(previous);
                if (remaining > TimeSpan.Zero)
                    await DelayAsync(remaining);
            }
            object body =
                _msgType == WorkWeiXinMsgType.markdown
                    ? new { msgtype = "markdown", markdown = new { content = part } }
                    : new { msgtype = "text", text = new { content = part } };
            using var request = new HttpRequestMessage(HttpMethod.Post, _webHookUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            finally
            {
                _lastAttempt = Stopwatch.GetTimestamp();
            }
            using (response)
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"企业微信通知返回 HTTP {(int)response.StatusCode}",
                        null,
                        response.StatusCode
                    );
                await ConfirmResponseAsync(response);
            }
        }
    }

    protected virtual Task DelayAsync(TimeSpan delay) => Task.Delay(delay);

    private static async Task ConfirmResponseAsync(HttpResponseMessage response)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (
                document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("errcode", out var value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetInt32(out int code)
            )
                throw new InvalidOperationException("企业微信通知未返回有效的确认结果");
            if (code != 0)
                throw new InvalidOperationException($"企业微信通知返回错误码 {code}");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("企业微信通知未返回有效的确认结果");
        }
    }

    public static IEnumerable<string> SplitMessage(string message, int byteLimit)
    {
        if (byteLimit < 4)
            throw new ArgumentOutOfRangeException(nameof(byteLimit));
        if (message.Length == 0)
        {
            yield return "";
            yield break;
        }
        int start = 0;
        while (start < message.Length)
        {
            int end = start;
            int bytes = 0;
            int newline = -1;
            while (end < message.Length)
            {
                if (!Rune.TryGetRuneAt(message, end, out Rune rune))
                    rune = Rune.ReplacementChar;
                if (bytes + rune.Utf8SequenceLength > byteLimit)
                    break;
                bytes += rune.Utf8SequenceLength;
                end += rune.Utf16SequenceLength;
                if (rune.Value == '\n')
                    newline = end;
            }
            if (end < message.Length)
            {
                if (newline > start)
                    end = newline;
                else if (end > start + 1 && message[end - 1] == '\r' && message[end] == '\n')
                    end--;
            }
            yield return message[start..end];
            start = end;
        }
    }

    public override void Dispose()
    {
        if (IsDisposed)
            return;
        try
        {
            base.Dispose();
        }
        finally
        {
            if (_ownsClient)
                _httpClient.Dispose();
        }
    }
}

public static class ChunkedWorkWeiXinLoggerConfigurationExtensions
{
    public static LoggerConfiguration ChunkedWorkWeiXinBatched(
        this LoggerSinkConfiguration sink,
        string webHookUrl,
        WorkWeiXinMsgType msgType = WorkWeiXinMsgType.text,
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
    ) =>
        sink.Sink(
            new ChunkedWorkWeiXinBatchedSink(
                webHookUrl,
                msgType,
                sendBatchesAsOneMessages,
                batchSizeLimit,
                outputTemplate,
                formatProvider,
                restrictedToMinimumLevel
            ),
            restrictedToMinimumLevel
        );
}
