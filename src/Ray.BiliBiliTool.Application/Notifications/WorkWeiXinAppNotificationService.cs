using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// 企业微信应用消息通知适配器：先以 corpId+secret 换取 access_token，再向成员发送 text 消息。
/// </summary>
public class WorkWeiXinAppNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory,
    ILogger<WorkWeiXinAppNotificationService> logger
) : INotificationService
{
    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => SendTextAsync(NotificationTextFormatter.FormatMessage(message), options));
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        var options = optionsMonitor.CurrentValue;
        if (!CanSend(options))
            return;

        await SafeSendAsync(() => SendTextAsync(NotificationTextFormatter.FormatSummary(title, lines), options));
    }

    /// <summary>使用指定配置发送一条测试通知（失败时向上抛出）</summary>
    public Task SendTestAsync(NotificationOptions options)
        => SendTextAsync("BiliTool 消息通知测试：如果你收到这条消息，说明企业微信应用配置正确 ✅", options);

    private static bool CanSend(NotificationOptions options)
        => options.IsWorkWeiXinAppEnabled
            && !string.IsNullOrWhiteSpace(options.WorkWeiXinCorpId)
            && !string.IsNullOrWhiteSpace(options.WorkWeiXinAgentId)
            && !string.IsNullOrWhiteSpace(options.WorkWeiXinSecret);

    private async Task SendTextAsync(string text, NotificationOptions options)
    {
        text = $"【{options.NotificationTitle}】\n{text}";
        var client = httpClientFactory.CreateClient(NotificationConstants.HttpClientName);

        // 1) 换取 access_token
        string tokenUrl =
            $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={Uri.EscapeDataString(options.WorkWeiXinCorpId!)}&corpsecret={Uri.EscapeDataString(options.WorkWeiXinSecret!)}";
        using (var tokenResponse = await client.GetAsync(tokenUrl))
        {
            tokenResponse.EnsureSuccessStatusCode();
            string body = await tokenResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            string accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException($"企业微信获取 access_token 失败：{body}");

            // 2) 发送消息
            string sendUrl = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={Uri.EscapeDataString(accessToken)}";
            var payload = new
            {
                touser = options.WorkWeiXinToUser,
                msgtype = "text",
                agentid = options.WorkWeiXinAgentId,
                text = new { content = text },
                safe = 0,
            };
            string json = JsonSerializer.Serialize(payload);
            using var sendContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var sendResponse = await client.PostAsync(sendUrl, sendContent);
            sendResponse.EnsureSuccessStatusCode();
        }

        logger.LogInformation("企业微信应用 通知发送成功");
    }

    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "企业微信应用 通知发送失败");
        }
    }
}
