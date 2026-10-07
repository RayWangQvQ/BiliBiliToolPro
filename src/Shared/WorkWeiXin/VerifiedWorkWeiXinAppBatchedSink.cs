using System.Globalization;
using System.Text;
using System.Text.Json;
using Ray.Serilog.Sinks.WorkWeiXinAppBatched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public class VerifiedWorkWeiXinAppBatchedSink : WorkWeiXinAppBatchedSink
{
    private readonly string _corpId;
    private readonly string _agentId;
    private readonly string _secret;
    private readonly string _toUser;
    private readonly string _toParty;
    private readonly string _toTag;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly bool _enabled;

    public VerifiedWorkWeiXinAppBatchedSink(
        string corpId,
        string agentId,
        string secret,
        string toUser = "",
        string toParty = "",
        string toTag = "",
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose,
        HttpClient? httpClient = null
    )
        : base(
            corpId,
            agentId,
            secret,
            toUser,
            toParty,
            toTag,
            sendBatchesAsOneMessages,
            batchSizeLimit,
            outputTemplate,
            formatProvider ?? CultureInfo.InvariantCulture,
            minimumLogEventLevel
        )
    {
        _corpId = corpId;
        _agentId = agentId;
        _secret = secret;
        _toUser = toUser;
        _toParty = toParty;
        _toTag = toTag;
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
        _enabled =
            !string.IsNullOrWhiteSpace(corpId)
            && !string.IsNullOrWhiteSpace(agentId)
            && !string.IsNullOrWhiteSpace(secret);
    }

    public override void Emit(LogEvent logEvent)
    {
        if (_enabled)
            base.Emit(logEvent);
    }

    protected override async Task PushMessageAsync(string message, string title = "推送")
    {
        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid="
                + Uri.EscapeDataString(_corpId)
                + "&corpsecret="
                + Uri.EscapeDataString(_secret)
        );
        using var tokenResponse = await SendConfirmedAsync(tokenRequest, "获取令牌");
        if (
            !tokenResponse.RootElement.TryGetProperty("access_token", out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString())
        )
            throw new InvalidOperationException("企业微信应用获取令牌未返回有效的访问令牌");
        string token = value.GetString()!;
        object body = new
        {
            touser = _toUser,
            toparty = _toParty,
            totag = _toTag,
            agentid = _agentId,
            msgtype = "text",
            text = new { content = message.Replace(Environment.NewLine, "\n") },
        };
        using var messageRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token="
                + Uri.EscapeDataString(token)
        )
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            ),
        };
        using var acknowledgement = await SendConfirmedAsync(messageRequest, "发送通知");
    }

    private async Task<JsonDocument> SendConfirmedAsync(HttpRequestMessage request, string stage)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException($"企业微信应用{stage}请求失败");
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException($"企业微信应用{stage}请求超时或已取消");
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"企业微信应用{stage}返回 HTTP {(int)response.StatusCode}",
                    null,
                    response.StatusCode
                );
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"企业微信应用{stage}未返回有效的确认结果");
            }
            catch (HttpRequestException)
            {
                throw new HttpRequestException($"企业微信应用{stage}读取响应失败");
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException($"企业微信应用{stage}读取响应超时或已取消");
            }
            if (
                document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("errcode", out var value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetInt32(out int code)
            )
            {
                document.Dispose();
                throw new InvalidOperationException($"企业微信应用{stage}未返回有效的确认结果");
            }
            if (code != 0)
            {
                document.Dispose();
                throw new InvalidOperationException($"企业微信应用{stage}返回错误码 {code}");
            }
            return document;
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

public static class VerifiedWorkWeiXinAppLoggerConfigurationExtensions
{
    public static LoggerConfiguration VerifiedWorkWeiXinAppBatched(
        this LoggerSinkConfiguration sink,
        string corpId,
        string agentId,
        string secret,
        string toUser = "",
        string toParty = "",
        string toTag = "",
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
    ) =>
        sink.Sink(
            new VerifiedWorkWeiXinAppBatchedSink(
                corpId,
                agentId,
                secret,
                toUser,
                toParty,
                toTag,
                sendBatchesAsOneMessages,
                batchSizeLimit,
                outputTemplate,
                formatProvider,
                restrictedToMinimumLevel
            ),
            restrictedToMinimumLevel
        );
}
