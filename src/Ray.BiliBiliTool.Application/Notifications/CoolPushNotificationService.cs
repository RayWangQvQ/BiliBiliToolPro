using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// CoolPush 通知适配器：GET 到 https://push.xuthus.cc/send/{sKey}?c=内容。
/// </summary>
public class CoolPushNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<CoolPushNotificationService> logger
) : INotificationService
{
    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => GetAsync(NotificationTextFormatter.FormatMessage(message), options));
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => GetAsync(NotificationTextFormatter.FormatSummary(title, lines), options));
    }

    /// <summary>使用指定配置发送一条测试通知（失败时向上抛出）</summary>
    public Task SendTestAsync(NotificationOptions options)
        => GetAsync("BiliTool 消息通知测试：如果你收到这条消息，说明 CoolPush 配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsCoolPushEnabled && !string.IsNullOrWhiteSpace(options.CoolPushSKey);

    private async Task GetAsync(string text, NotificationOptions options)
    {
        text = $"【{options.NotificationTitle}】\n{text}";
        string url =
            $"https://push.xuthus.cc/send/{Uri.EscapeDataString(options.CoolPushSKey!)}?c={Uri.EscapeDataString(text)}";

        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("CoolPush 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CoolPush 通知发送失败");
        }
    }
}
