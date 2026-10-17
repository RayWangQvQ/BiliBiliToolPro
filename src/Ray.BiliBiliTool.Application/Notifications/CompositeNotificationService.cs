using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// 消息通知调度器：聚合本地日志(Serilog) + Telegram + 自定义Webhook + PushPlus + Server酱 + 钉钉
/// + 企业微信(群机器人/应用) + CoolPush + Microsoft Teams + Gotify 等通道，
/// 由 <see cref="NotificationOptions.IsEnable"/> 总开关控制是否发送，各通道独立启停、可同时启用。
/// 对外以 <see cref="INotificationService"/> 暴露。
/// </summary>
public class CompositeNotificationService(
    IOptionsMonitor<NotificationOptions> optionsMonitor,
    SerilogNotificationAdapter serilogAdapter,
    TelegramNotificationService telegram,
    CustomWebhookNotificationService webhook,
    PushPlusNotificationService pushPlus,
    ServerChanNotificationService serverChan,
    DingTalkNotificationService dingTalk,
    WorkWeiXinNotificationService workWeiXin,
    WorkWeiXinAppNotificationService workWeiXinApp,
    CoolPushNotificationService coolPush,
    MicrosoftTeamsNotificationService teams,
    GotifyNotificationService gotify,
    ILogger<CompositeNotificationService> logger
) : INotificationService
{
    private readonly INotificationService[] _channels =
    [
        serilogAdapter,
        telegram,
        webhook,
        pushPlus,
        serverChan,
        dingTalk,
        workWeiXin,
        workWeiXinApp,
        coolPush,
        teams,
        gotify,
    ];

    public async Task SendAsync(NotificationMessage message, string groupKey)
    {
        if (!IsEnabled())
            return;

        foreach (INotificationService channel in _channels)
        {
            await SafeDispatchAsync(channel, c => c.SendAsync(message, groupKey));
        }
    }

    public async Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey)
    {
        if (!IsEnabled())
            return;

        foreach (INotificationService channel in _channels)
        {
            await SafeDispatchAsync(channel, c => c.SendSummaryAsync(title, lines, groupKey));
        }
    }

    private bool IsEnabled() => optionsMonitor.CurrentValue.IsEnable;

    private async Task SafeDispatchAsync(
        INotificationService channel,
        Func<INotificationService, Task> send
    )
    {
        try
        {
            await send(channel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "消息通知通道发送失败: {Channel}", channel.GetType().Name);
        }
    }
}
