namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 消息通知配置。总开关为 <see cref="BaseConfigOptions.IsEnable"/>，
/// 各通道（Telegram、自定义Webhook）可独立启停，并支持同时启用。
/// </summary>
public class NotificationOptions : BaseConfigOptions
{
    public override string SectionName => "NotificationConfig";

    #region Telegram 机器人

    /// <summary>
    /// 是否启用 Telegram 机器人通知
    /// </summary>
    public bool IsTelegramEnabled { get; set; }

    /// <summary>
    /// Telegram Bot Token（向 @BotFather 创建机器人后获取）
    /// </summary>
    public string? TelegramBotToken { get; set; }

    /// <summary>
    /// Telegram Chat ID（接收消息的对话 ID）
    /// </summary>
    public string? TelegramChatId { get; set; }

    /// <summary>
    /// Telegram API 地址，默认为 https://api.telegram.org，可替换为自己搭建的反代地址
    /// </summary>
    public string TelegramApiHost { get; set; } = "https://api.telegram.org";

    #endregion

    #region 自定义 Webhook

    /// <summary>
    /// 是否启用自定义 Webhook 通知
    /// </summary>
    public bool IsWebhookEnabled { get; set; }

    /// <summary>
    /// 自定义 Webhook 地址（兼容 Bark、飞书自定义机器人、自建服务等支持 JSON POST 的通道）
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// 发送时的 Content-Type，默认为 application/json
    /// </summary>
    public string WebhookContentType { get; set; } = "application/json";

    #endregion

    #region 通知时机

    /// <summary>
    /// 每日任务执行成功后是否发送执行摘要
    /// </summary>
    public bool SendSummaryOnSuccess { get; set; } = true;

    /// <summary>
    /// 每日任务执行失败时是否发送错误通知
    /// </summary>
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
                { $"{SectionName}:{nameof(SendSummaryOnSuccess)}", SendSummaryOnSuccess.ToString().ToLower() },
                { $"{SectionName}:{nameof(SendOnError)}", SendOnError.ToString().ToLower() },
            }
        );
    }
}
