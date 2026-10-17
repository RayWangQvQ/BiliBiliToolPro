using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// Gotify 通知适配器：向自建 Gotify 服务 POST message 消息。
/// </summary>
public class GotifyNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<GotifyNotificationService> logger
) : INotificationService
{
    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => PostAsync(options.NotificationTitle, NotificationTextFormatter.FormatMessage(message), options));
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => PostAsync(options.NotificationTitle, NotificationTextFormatter.FormatSummary(title, lines), options));
    }

    /// <summary>使用指定配置发送一条测试通知（失败时向上抛出）</summary>
    public Task SendTestAsync(NotificationOptions options)
        => PostAsync(options.NotificationTitle, "如果你收到这条消息，说明 Gotify 配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsGotifyEnabled
            && !string.IsNullOrWhiteSpace(options.GotifyHost)
            && !string.IsNullOrWhiteSpace(options.GotifyToken);

    private async Task PostAsync(string title, string text, NotificationOptions options)
    {
        string host = options.GotifyHost!.TrimEnd('/');
        string url = $"{host}/message?token={Uri.EscapeDataString(options.GotifyToken!)}";
        string json = JsonSerializer.Serialize(new { title, message = text });

        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Gotify 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gotify 通知发送失败");
        }
    }
}
