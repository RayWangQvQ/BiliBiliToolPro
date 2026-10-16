using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// Microsoft Teams 通知适配器：向团队连接器 Webhook POST text 消息。
/// </summary>
public class MicrosoftTeamsNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<MicrosoftTeamsNotificationService> logger
) : INotificationService
{
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

    /// <summary>使用指定配置发送一条测试通知（失败时向上抛出）</summary>
    public Task SendTestAsync(NotificationOptions options)
        => PostAsync("BiliTool 消息通知测试：如果你收到这条消息，说明 Microsoft Teams 配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsTeamsEnabled && !string.IsNullOrWhiteSpace(options.TeamsWebhook);

    private async Task PostAsync(string text, NotificationOptions options)
    {
        text = $"【{options.NotificationTitle}】\n{text}";
        string json = JsonSerializer.Serialize(new { text });
        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(options.TeamsWebhook, content);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Microsoft Teams 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Microsoft Teams 通知发送失败");
        }
    }
}
