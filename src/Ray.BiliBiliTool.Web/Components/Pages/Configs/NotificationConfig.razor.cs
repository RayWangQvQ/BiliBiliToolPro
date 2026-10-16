using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class NotificationConfig : BaseConfigComponent<NotificationOptions>
{
    [Inject]
    private IOptionsMonitor<NotificationOptions> NotificationOptionsMonitor { get; set; } = null!;

    [Inject]
    private TelegramNotificationService TelegramNotificationService { get; set; } = null!;

    [Inject]
    private CustomWebhookNotificationService WebhookNotificationService { get; set; } = null!;

    protected override IOptionsMonitor<NotificationOptions> OptionsMonitor =>
        NotificationOptionsMonitor;

    private MarkupString? _testMessage;
    private bool _testSuccess;

    private async Task SendTestNotificationAsync()
    {
        _testMessage = null;
        _testSuccess = false;

        if (!_config.IsEnable)
        {
            _testMessage = new MarkupString("请先开启「启用消息通知」再测试发送。");
            return;
        }

        List<string> sentChannels = [];

        try
        {
            if (_config.IsTelegramEnabled)
            {
                await TelegramNotificationService.SendTestAsync(_config);
                sentChannels.Add("Telegram");
            }

            if (_config.IsWebhookEnabled)
            {
                await WebhookNotificationService.SendTestAsync(_config);
                sentChannels.Add("自定义Webhook");
            }
        }
        catch (Exception ex)
        {
            _testSuccess = false;
            _testMessage = new MarkupString($"测试通知发送失败：{ex.Message}");
            return;
        }

        _testSuccess = sentChannels.Count > 0;
        _testMessage = sentChannels.Count > 0
            ? new MarkupString($"测试通知已发送到：{string.Join("、", sentChannels)}")
            : new MarkupString("未启用任何通知通道，请先勾选 Telegram 或自定义 Webhook。");
    }
}
