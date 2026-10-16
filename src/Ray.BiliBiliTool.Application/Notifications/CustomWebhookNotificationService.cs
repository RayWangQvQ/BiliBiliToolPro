using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// 自定义 Webhook 通知适配器：向用户配置的任意 Webhook 地址 POST 一条 JSON 通知。
/// 兼容 Bark、飞书自定义机器人、自建服务等支持 JSON POST 的通道。
/// </summary>
public class CustomWebhookNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<CustomWebhookNotificationService> logger
) : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        var payload = new WebhookPayload
        {
            Title = message.Title,
            Body = message.Body,
            Text = NotificationTextFormatter.FormatMessage(message),
            Level = message.Level.ToString(),
            GroupKey = groupKey,
        };

        await SafeSendAsync(() => PostAsync(payload, options));
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        var payload = new WebhookPayload
        {
            Title = title,
            Text = NotificationTextFormatter.FormatSummary(title, lines),
            Level = NotificationLevel.Info.ToString(),
            GroupKey = groupKey,
            Lines = lines
                .Select(line => new WebhookLine(line.Label, line.Value, line.StatusIcon))
                .ToArray(),
        };

        await SafeSendAsync(() => PostAsync(payload, options));
    }

    /// <summary>
    /// 使用指定的配置发送一条测试通知（供 Web 后台"发送测试通知"按钮调用，失败时向上抛出）。
    /// </summary>
    public Task SendTestAsync(NotificationOptions options)
    {
        var payload = new WebhookPayload
        {
            Title = "BiliTool 消息通知测试",
            Text = "BiliTool 消息通知测试：如果你收到这条消息，说明 Webhook 配置正确 ✅",
            Level = NotificationLevel.Info.ToString(),
            GroupKey = "test",
        };

        return PostAsync(payload, options);
    }

    private static bool CanSend(NotificationOptions options)
        => options.IsWebhookEnabled && !string.IsNullOrWhiteSpace(options.WebhookUrl);

    private async Task PostAsync(WebhookPayload payload, NotificationOptions options)
    {
        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        string contentType = string.IsNullOrWhiteSpace(options.WebhookContentType)
            ? "application/json"
            : options.WebhookContentType;
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, contentType);
        using var response = await client.PostAsync(options.WebhookUrl, content);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Webhook 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook 通知发送失败");
        }
    }

    private sealed record WebhookLine(string Label, string Value, string? StatusIcon);

    private sealed class WebhookPayload
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Text { get; set; }
        public string? Level { get; set; }
        public string? GroupKey { get; set; }
        public WebhookLine[]? Lines { get; set; }
    }
}
