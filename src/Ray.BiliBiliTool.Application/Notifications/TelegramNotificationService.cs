using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// Telegram 机器人通知适配器：通过 Bot API sendMessage 接口推送任务通知。
/// </summary>
public class TelegramNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<TelegramNotificationService> logger
) : INotificationService
{
    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(
            () => SendTextAsync(NotificationTextFormatter.FormatMessage(message), options)
        );
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(
            () => SendTextAsync(NotificationTextFormatter.FormatSummary(title, lines), options)
        );
    }

    /// <summary>
    /// 使用指定的配置发送一条测试通知（供 Web 后台"发送测试通知"按钮调用，失败时向上抛出）。
    /// </summary>
    public Task SendTestAsync(NotificationOptions options)
        => SendTextAsync("BiliTool 消息通知测试：如果你收到这条消息，说明 Telegram 配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsTelegramEnabled
            && !string.IsNullOrWhiteSpace(options.TelegramBotToken)
            && !string.IsNullOrWhiteSpace(options.TelegramChatId);

    private async Task SendTextAsync(string text, NotificationOptions options)
    {
        string host = string.IsNullOrWhiteSpace(options.TelegramApiHost)
            ? "https://api.telegram.org"
            : options.TelegramApiHost.TrimEnd('/');
        string url = $"{host}/bot{options.TelegramBotToken}/sendMessage";

        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["chat_id"] = options.TelegramChatId!,
                ["text"] = text,
                ["disable_web_page_preview"] = "true",
            }
        );

        using var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Telegram 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Telegram 通知发送失败");
        }
    }
}
