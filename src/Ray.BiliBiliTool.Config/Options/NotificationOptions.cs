namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 消息通知配置。总开关为 <see cref="BaseConfigOptions.IsEnable"/>，
/// 各通道（Telegram、自定义Webhook、PushPlus、Server酱、钉钉、企业微信等）可独立启停，并支持同时启用。
/// </summary>
public class NotificationOptions : BaseConfigOptions
{
    public override string SectionName => "NotificationConfig";

    #region Telegram 机器人

    /// <summary>是否启用 Telegram 机器人通知</summary>
    public bool IsTelegramEnabled { get; set; }

    /// <summary>Telegram Bot Token（向 @BotFather 创建机器人后获取）</summary>
    public string? TelegramBotToken { get; set; }

    /// <summary>Telegram Chat ID（接收消息的对话 ID）</summary>
    public string? TelegramChatId { get; set; }

    /// <summary>Telegram API 地址，默认为 https://api.telegram.org，可替换为自己搭建的反代地址</summary>
    public string TelegramApiHost { get; set; } = "https://api.telegram.org";

    #endregion

    #region 自定义 Webhook

    /// <summary>是否启用自定义 Webhook 通知</summary>
    public bool IsWebhookEnabled { get; set; }

    /// <summary>自定义 Webhook 地址（兼容 Bark、飞书自定义机器人、自建服务等支持 JSON POST 的通道）</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>发送时的 Content-Type，默认为 application/json</summary>
    public string WebhookContentType { get; set; } = "application/json";

    #endregion

    #region PushPlus

    /// <summary>是否启用 PushPlus 通知</summary>
    public bool IsPushPlusEnabled { get; set; }

    /// <summary>PushPlus Token（https://www.pushplus.plus 个人中心获取）</summary>
    public string? PushPlusToken { get; set; }

    /// <summary>PushPlus 渠道，值域[wechat,webhook,cp,sms,mail]，分别对应[微信公众号、指定第三方webhook、企业微信应用、短信、邮件]</summary>
    public string PushPlusChannel { get; set; } = "";

    /// <summary>PushPlus 群组编码，用于群发，没有就不填；channel 为 webhook 时无效</summary>
    public string PushPlusTopic { get; set; } = "";

    /// <summary>PushPlus webhook 编码（不是地址），仅 channel 使用 webhook 和 cp 渠道时需要填写</summary>
    public string PushPlusWebhook { get; set; } = "";

    #endregion

    #region Server酱

    /// <summary>是否启用 Server酱（Turbo）通知</summary>
    public bool IsServerChanEnabled { get; set; }

    /// <summary>Server酱 Turbo SendKey（sct.ftqq.com）</summary>
    public string? ServerChanSendKey { get; set; }

    /// <summary>Server酱 老版 SCKEY（sc.ftqq.com，已过时，留空即可）</summary>
    public string? ServerChanScKey { get; set; }

    #endregion

    #region 钉钉

    /// <summary>是否启用钉钉通知</summary>
    public bool IsDingTalkEnabled { get; set; }

    /// <summary>钉钉自定义机器人 Webhook 地址（群机器人安全设置里获取）</summary>
    public string? DingTalkWebhook { get; set; }

    #endregion

    #region 企业微信群机器人

    /// <summary>是否启用企业微信群机器人通知</summary>
    public bool IsWorkWeiXinEnabled { get; set; }

    /// <summary>企业微信群机器人 Webhook 地址</summary>
    public string? WorkWeiXinWebhook { get; set; }

    #endregion

    #region 企业微信应用

    /// <summary>是否启用企业微信应用消息通知</summary>
    public bool IsWorkWeiXinAppEnabled { get; set; }

    /// <summary>企业微信 Corp ID</summary>
    public string? WorkWeiXinCorpId { get; set; }

    /// <summary>企业微信应用 Agent ID</summary>
    public string? WorkWeiXinAgentId { get; set; }

    /// <summary>企业微信应用 Secret</summary>
    public string? WorkWeiXinSecret { get; set; }

    /// <summary>接收消息的成员（@all 表示全员）</summary>
    public string WorkWeiXinToUser { get; set; } = "@all";

    #endregion

    #region CoolPush

    /// <summary>是否启用 CoolPush 通知</summary>
    public bool IsCoolPushEnabled { get; set; }

    /// <summary>CoolPush SKey（https://push.xuthus.cc 注册获取）</summary>
    public string? CoolPushSKey { get; set; }

    #endregion

    #region Microsoft Teams

    /// <summary>是否启用 Microsoft Teams 通知</summary>
    public bool IsTeamsEnabled { get; set; }

    /// <summary>Microsoft Teams 团队 Webhook 完整地址</summary>
    public string? TeamsWebhook { get; set; }

    #endregion

    #region Gotify

    /// <summary>是否启用 Gotify 通知</summary>
    public bool IsGotifyEnabled { get; set; }

    /// <summary>Gotify 服务地址（如 https://gotify.example.com，不带末尾斜杠）</summary>
    public string? GotifyHost { get; set; }

    /// <summary>Gotify 应用 Token</summary>
    public string? GotifyToken { get; set; }

    #endregion

    #region 推送标题

    /// <summary>
    /// 自定义推送标题（所有渠道发送时的消息标题，默认“BiliTool 消息通知”）。
    /// 纯文本渠道（Telegram、钉钉、企业微信、Teams、CoolPush）会把标题作为正文首行。
    /// </summary>
    public string NotificationTitle { get; set; } = "BiliTool 消息通知";

    #endregion

    #region 通知时机

    /// <summary>每日任务执行成功后是否发送执行摘要</summary>
    public bool SendSummaryOnSuccess { get; set; } = true;

    /// <summary>每日任务执行失败时是否发送错误通知</summary>
    public bool SendOnError { get; set; } = true;

    #endregion

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(IsTelegramEnabled)}", IsTelegramEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(TelegramBotToken)}", TelegramBotToken ?? "" },
                { $"{SectionName}:{nameof(TelegramChatId)}", TelegramChatId ?? "" },
                { $"{SectionName}:{nameof(TelegramApiHost)}", TelegramApiHost },

                { $"{SectionName}:{nameof(IsWebhookEnabled)}", IsWebhookEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(WebhookUrl)}", WebhookUrl ?? "" },
                { $"{SectionName}:{nameof(WebhookContentType)}", WebhookContentType },

                { $"{SectionName}:{nameof(IsPushPlusEnabled)}", IsPushPlusEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(PushPlusToken)}", PushPlusToken ?? "" },
                { $"{SectionName}:{nameof(PushPlusChannel)}", PushPlusChannel },
                { $"{SectionName}:{nameof(PushPlusTopic)}", PushPlusTopic },
                { $"{SectionName}:{nameof(PushPlusWebhook)}", PushPlusWebhook },

                { $"{SectionName}:{nameof(IsServerChanEnabled)}", IsServerChanEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(ServerChanSendKey)}", ServerChanSendKey ?? "" },
                { $"{SectionName}:{nameof(ServerChanScKey)}", ServerChanScKey ?? "" },

                { $"{SectionName}:{nameof(IsDingTalkEnabled)}", IsDingTalkEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(DingTalkWebhook)}", DingTalkWebhook ?? "" },

                { $"{SectionName}:{nameof(IsWorkWeiXinEnabled)}", IsWorkWeiXinEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(WorkWeiXinWebhook)}", WorkWeiXinWebhook ?? "" },

                { $"{SectionName}:{nameof(IsWorkWeiXinAppEnabled)}", IsWorkWeiXinAppEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(WorkWeiXinCorpId)}", WorkWeiXinCorpId ?? "" },
                { $"{SectionName}:{nameof(WorkWeiXinAgentId)}", WorkWeiXinAgentId ?? "" },
                { $"{SectionName}:{nameof(WorkWeiXinSecret)}", WorkWeiXinSecret ?? "" },
                { $"{SectionName}:{nameof(WorkWeiXinToUser)}", WorkWeiXinToUser },

                { $"{SectionName}:{nameof(IsCoolPushEnabled)}", IsCoolPushEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(CoolPushSKey)}", CoolPushSKey ?? "" },

                { $"{SectionName}:{nameof(IsTeamsEnabled)}", IsTeamsEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(TeamsWebhook)}", TeamsWebhook ?? "" },

                { $"{SectionName}:{nameof(IsGotifyEnabled)}", IsGotifyEnabled.ToString().ToLower() },
                { $"{SectionName}:{nameof(GotifyHost)}", GotifyHost ?? "" },
                { $"{SectionName}:{nameof(GotifyToken)}", GotifyToken ?? "" },

                { $"{SectionName}:{nameof(NotificationTitle)}", NotificationTitle },
                { $"{SectionName}:{nameof(SendSummaryOnSuccess)}", SendSummaryOnSuccess.ToString().ToLower() },
                { $"{SectionName}:{nameof(SendOnError)}", SendOnError.ToString().ToLower() },
            }
        );
    }
}
