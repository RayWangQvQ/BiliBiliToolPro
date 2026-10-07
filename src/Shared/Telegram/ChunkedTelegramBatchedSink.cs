using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Ray.Serilog.Sinks.TelegramBatched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public class ChunkedTelegramBatchedSink : TelegramBatchedSink
{
    private readonly string _chatId;
    private readonly Uri? _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly bool _enabled;
    private long? _lastAttempt;

    public ChunkedTelegramBatchedSink(
        string botToken,
        string chatId,
        string proxy = "",
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose,
        HttpClient? httpClient = null
    )
        : base(
            ValidateConfiguration(botToken, chatId, proxy, httpClient),
            chatId,
            proxy,
            sendBatchesAsOneMessages,
            batchSizeLimit,
            outputTemplate,
            formatProvider ?? CultureInfo.InvariantCulture,
            minimumLogEventLevel
        )
    {
        _chatId = chatId;
        _enabled = !string.IsNullOrWhiteSpace(botToken) && !string.IsNullOrWhiteSpace(chatId);
        if (_enabled)
        {
            if (
                !Uri.TryCreate(
                    $"https://api.telegram.org/bot{botToken}/sendMessage",
                    UriKind.Absolute,
                    out var apiUrl
                )
            )
                throw new ArgumentException("Telegram BotToken 格式无效", nameof(botToken));
            _apiUrl = apiUrl;
        }
        _httpClient = httpClient ?? CreateClient(_enabled ? proxy : "");
        _ownsClient = httpClient is null;
    }

    public override void Emit(LogEvent logEvent)
    {
        if (_enabled)
            base.Emit(logEvent);
    }

    protected override async Task PushMessageAsync(string message, string title = "推送")
    {
        title ??= "";
        title = title.Length <= 256 ? title : SplitMessage(title, 256).First();
        var capacity = 4096 - title.Length - 2 * Environment.NewLine.Length;
        foreach (var part in SplitMessage(message, capacity))
        {
            await WaitForSendAsync();
            try
            {
                await SendPartAsync(WebUtility.HtmlEncode(part), WebUtility.HtmlEncode(title));
            }
            finally
            {
                _lastAttempt = Stopwatch.GetTimestamp();
            }
        }
    }

    protected virtual async Task WaitForSendAsync()
    {
        if (_lastAttempt is long previous)
        {
            var remaining = TimeSpan.FromMilliseconds(1100) - Stopwatch.GetElapsedTime(previous);
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);
        }
    }

    protected virtual async Task SendPartAsync(string message, string title)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new
                    {
                        chat_id = _chatId,
                        text = $"<b>{title}</b>{Environment.NewLine}{Environment.NewLine}{message}",
                        parse_mode = "HTML",
                        disable_web_page_preview = true,
                    }
                ),
                Encoding.UTF8,
                "application/json"
            ),
        };
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException exception)
        {
            throw new HttpRequestException("Telegram 通知请求失败", null, exception.StatusCode);
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException("Telegram 通知请求超时或已取消");
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Telegram 通知返回 HTTP {(int)response.StatusCode}",
                    null,
                    response.StatusCode
                );
            try
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (
                    document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("ok", out var result)
                    || result.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                )
                    throw new InvalidOperationException("Telegram 通知未返回有效的确认结果");
                if (!result.GetBoolean())
                {
                    if (
                        document.RootElement.TryGetProperty("error_code", out var value)
                        && value.ValueKind == JsonValueKind.Number
                        && value.TryGetInt32(out int code)
                    )
                        throw new InvalidOperationException($"Telegram 通知返回错误码 {code}");
                    throw new InvalidOperationException("Telegram 通知被拒绝");
                }
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Telegram 通知未返回有效的确认结果");
            }
        }
    }

    private static HttpClient CreateClient(string proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
            return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        return new HttpClient(new HttpClientHandler { Proxy = CreateProxy(proxy), UseProxy = true })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    private static string ValidateConfiguration(
        string botToken,
        string chatId,
        string proxy,
        HttpClient? httpClient
    )
    {
        if (!string.IsNullOrWhiteSpace(botToken) && !string.IsNullOrWhiteSpace(chatId))
        {
            if (
                !Uri.TryCreate(
                    $"https://api.telegram.org/bot{botToken}/sendMessage",
                    UriKind.Absolute,
                    out _
                )
            )
                throw new ArgumentException("Telegram BotToken 格式无效", nameof(botToken));
            if (httpClient is null && !string.IsNullOrWhiteSpace(proxy))
                _ = CreateProxy(proxy);
        }
        return botToken;
    }

    private static WebProxy CreateProxy(string proxy)
    {
        string address = proxy.Trim();
        if (!address.Contains("://", StringComparison.Ordinal))
            address = "http://" + address;
        if (
            !Uri.TryCreate(address, UriKind.Absolute, out var uri)
            || string.IsNullOrEmpty(uri.Host)
            || uri.Scheme is not ("http" or "https" or "socks4" or "socks4a" or "socks5")
        )
            throw new ArgumentException("Telegram 代理地址无效", nameof(proxy));
        var location = new UriBuilder(uri) { UserName = "", Password = "" }.Uri;
        var webProxy = new WebProxy(location, true);
        if (uri.UserInfo.Length > 0)
        {
            int separator = uri.UserInfo.IndexOf(':');
            webProxy.Credentials = new NetworkCredential(
                Uri.UnescapeDataString(separator < 0 ? uri.UserInfo : uri.UserInfo[..separator]),
                separator < 0 ? "" : Uri.UnescapeDataString(uri.UserInfo[(separator + 1)..])
            );
        }
        return webProxy;
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
