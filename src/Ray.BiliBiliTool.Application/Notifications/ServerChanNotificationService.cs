using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// Server酱（ServerChan）通知适配器：优先使用 Turbo（sctapi.ftqq.com），否则使用老版（sc.ftqq.com）。
/// </summary>
public class ServerChanNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<ServerChanNotificationService> logger
) : INotificationService
{
    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => GetAsync(options.NotificationTitle, NotificationTextFormatter.FormatMessage(message), options));
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => GetAsync(options.NotificationTitle, NotificationTextFormatter.FormatSummary(title, lines), options));
    }

    /// <summary>使用指定配置发送一条测试通知（失败时向上抛出）</summary>
    public Task SendTestAsync(NotificationOptions options)
        => GetAsync(options.NotificationTitle, "如果你收到这条消息，说明 Server酱 配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsServerChanEnabled
            && (!string.IsNullOrWhiteSpace(options.ServerChanSendKey)
                || !string.IsNullOrWhiteSpace(options.ServerChanScKey));

    private async Task GetAsync(string title, string text, NotificationOptions options)
    {
        string titleEnc = Uri.EscapeDataString(title);
        string textEnc = Uri.EscapeDataString(text);

        string url;
        if (!string.IsNullOrWhiteSpace(options.ServerChanSendKey))
        {
            url = $"https://sctapi.ftqq.com/{options.ServerChanSendKey}.send?title={titleEnc}&desp={textEnc}";
        }
        else
        {
            url = $"https://sc.ftqq.com/{options.ServerChanScKey}.send?title={titleEnc}&desp={textEnc}";
        }

        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Server酱 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server酱 通知发送失败");
        }
    }
}
