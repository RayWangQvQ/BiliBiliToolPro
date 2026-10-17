using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// PushPlus 通知适配器：POST 到 https://www.pushplus.plus/send，支持微信公众号、webhook、短信等渠道。
/// </summary>
public class PushPlusNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<PushPlusNotificationService> logger
) : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => PostAsync(NotificationTextFormatter.FormatMessage(message), options));
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => PostAsync(NotificationTextFormatter.FormatSummary(title, lines), options));
    }

    /// <summary>使用指定配置发送一条测试通知（供 Web 后台测试按钮调用，失败时向上抛出）</summary>
    public Task SendTestAsync(NotificationOptions options)
        => PostAsync("BiliTool 消息通知测试：如果你收到这条消息，说明 PushPlus 配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsPushPlusEnabled && !string.IsNullOrWhiteSpace(options.PushPlusToken);

    private async Task PostAsync(string text, NotificationOptions options)
    {
        var payload = new Dictionary<string, string>
        {
            ["token"] = options.PushPlusToken!,
            ["title"] = options.NotificationTitle,
            ["content"] = text,
            ["template"] = "txt",
        };
        if (!string.IsNullOrWhiteSpace(options.PushPlusChannel))
            payload["channel"] = options.PushPlusChannel;
        if (!string.IsNullOrWhiteSpace(options.PushPlusTopic))
            payload["topic"] = options.PushPlusTopic;
        if (!string.IsNullOrWhiteSpace(options.PushPlusWebhook))
            payload["webhook"] = options.PushPlusWebhook;

        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("https://www.pushplus.plus/send", content);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("PushPlus 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PushPlus 通知发送失败");
        }
    }
}
