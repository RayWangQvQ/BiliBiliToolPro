using System.Globalization;
using System.Text;
using System.Text.Json;
using Ray.Serilog.Sinks.Batched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public sealed class HttpNotificationBatchedSink : BatchedSink
{
    private readonly NotificationPushService _sender;
    private readonly bool _enabled;

    public HttpNotificationBatchedSink(
        string api,
        string? token,
        string? bodyJsonTemplate,
        string placeholder,
        Dictionary<string, string>? headers = null,
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string? outputTemplate = null,
        IFormatProvider? formatProvider = null,
        LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose,
        HttpClient? httpClient = null
    )
        : base(
            sendBatchesAsOneMessages,
            batchSizeLimit,
            outputTemplate,
            formatProvider ?? CultureInfo.InvariantCulture,
            minimumLogEventLevel
        )
    {
        _enabled =
            !string.IsNullOrWhiteSpace(api) && (token is null || !string.IsNullOrWhiteSpace(token));
        _sender = new NotificationPushService(
            api,
            token,
            bodyJsonTemplate,
            placeholder,
            headers,
            httpClient
        );
    }

    protected override IPushService PushService => _sender;

    public override void Emit(LogEvent logEvent)
    {
        if (_enabled)
            base.Emit(logEvent);
    }

    protected override async Task PushMessageAsync(string message, string title = "推送")
    {
        using var response = await _sender.PushMessageAsync(message, title);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"通知服务返回 HTTP {(int)response.StatusCode}",
                null,
                response.StatusCode
            );
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
            _sender.Dispose();
        }
    }
}

public sealed class NotificationPushService : PushService, IDisposable
{
    private readonly HttpClient _client;
    private readonly bool _ownsClient;
    private readonly string _api;
    private readonly string? _token;
    private readonly string? _template;
    private readonly string _placeholder;
    private readonly Dictionary<string, string> _headers;

    public NotificationPushService(
        string api,
        string? token,
        string? template,
        string placeholder,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null
    )
    {
        _api = api;
        _token = token;
        _template = template;
        _placeholder = placeholder;
        _headers = headers is null ? new() : new(headers, StringComparer.OrdinalIgnoreCase);
        _client = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
    }

    protected override string ClientName => _token is null ? "自定义" : "Gotify";

    protected override async Task<HttpResponseMessage> DoSendAsync(
        string message,
        string title = ""
    )
    {
        string body;
        if (_token is not null)
        {
            body = JsonSerializer.Serialize(
                new
                {
                    title = string.IsNullOrWhiteSpace(title) ? "推送" : title,
                    message = message.Replace(Environment.NewLine, "\n"),
                    extras = new Dictionary<string, object>
                    {
                        ["client::display"] = new { contentType = "text/markdown" },
                    },
                }
            );
        }
        else
        {
            if (string.IsNullOrEmpty(_placeholder))
                throw new ArgumentException("自定义通知占位符不能为空");
            body = (_template ?? "").Replace(_placeholder, JsonSerializer.Serialize(message));
            using var document = JsonDocument.Parse(body);
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            _token is null ? _api : _api.TrimEnd('/') + "/message"
        );
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        if (_token is not null)
            request.Headers.Add("X-Gotify-Key", _token);
        foreach (var header in _headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                request.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(header.Value);
            else
            {
                try
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                catch (InvalidOperationException)
                {
                    request.Content.Headers.Add(header.Key, header.Value);
                }
            }
        }
        return await _client.SendAsync(request);
    }

    public void Dispose()
    {
        if (_ownsClient)
            _client.Dispose();
    }
}

public static class HttpNotificationLoggerConfigurationExtensions
{
    public static LoggerConfiguration CompatibleGotifyBatched(
        this LoggerSinkConfiguration sink,
        string host,
        string token,
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string? outputTemplate = null,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
    ) =>
        sink.Sink(
            new HttpNotificationBatchedSink(
                host,
                token,
                null,
                "",
                null,
                sendBatchesAsOneMessages,
                batchSizeLimit,
                outputTemplate,
                formatProvider,
                restrictedToMinimumLevel
            ),
            restrictedToMinimumLevel
        );

    public static LoggerConfiguration HeaderOtherApiBatched(
        this LoggerSinkConfiguration sink,
        string api,
        string bodyJsonTemplate,
        string placeholder,
        Dictionary<string, string>? headers = null,
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
    ) =>
        sink.Sink(
            new HttpNotificationBatchedSink(
                api,
                null,
                bodyJsonTemplate,
                placeholder,
                headers,
                sendBatchesAsOneMessages,
                batchSizeLimit,
                null,
                formatProvider,
                restrictedToMinimumLevel
            ),
            restrictedToMinimumLevel
        );
}
