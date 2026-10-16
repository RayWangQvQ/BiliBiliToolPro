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

    [Inject]
    private PushPlusNotificationService PushPlusNotificationService { get; set; } = null!;

    [Inject]
    private ServerChanNotificationService ServerChanNotificationService { get; set; } = null!;

    [Inject]
    private DingTalkNotificationService DingTalkNotificationService { get; set; } = null!;

    [Inject]
    private WorkWeiXinNotificationService WorkWeiXinNotificationService { get; set; } = null!;

    [Inject]
    private WorkWeiXinAppNotificationService WorkWeiXinAppNotificationService { get; set; } = null!;

    [Inject]
    private CoolPushNotificationService CoolPushNotificationService { get; set; } = null!;

    [Inject]
    private MicrosoftTeamsNotificationService TeamsNotificationService { get; set; } = null!;

    [Inject]
    private GotifyNotificationService GotifyNotificationService { get; set; } = null!;

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

            if (_config.IsPushPlusEnabled)
            {
                await PushPlusNotificationService.SendTestAsync(_config);
                sentChannels.Add("PushPlus");
            }

            if (_config.IsServerChanEnabled)
            {
                await ServerChanNotificationService.SendTestAsync(_config);
                sentChannels.Add("Server酱");
            }

            if (_config.IsDingTalkEnabled)
            {
                await DingTalkNotificationService.SendTestAsync(_config);
                sentChannels.Add("钉钉");
            }

            if (_config.IsWorkWeiXinEnabled)
            {
                await WorkWeiXinNotificationService.SendTestAsync(_config);
                sentChannels.Add("企业微信群机器人");
            }

            if (_config.IsWorkWeiXinAppEnabled)
            {
                await WorkWeiXinAppNotificationService.SendTestAsync(_config);
                sentChannels.Add("企业微信应用");
            }

            if (_config.IsCoolPushEnabled)
            {
                await CoolPushNotificationService.SendTestAsync(_config);
                sentChannels.Add("CoolPush");
            }

            if (_config.IsTeamsEnabled)
            {
                await TeamsNotificationService.SendTestAsync(_config);
                sentChannels.Add("Microsoft Teams");
            }

            if (_config.IsGotifyEnabled)
            {
                await GotifyNotificationService.SendTestAsync(_config);
                sentChannels.Add("Gotify");
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
            : new MarkupString("未启用任何通知通道，请至少勾选一个通知渠道。");
    }
}
