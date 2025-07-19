namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 粉丝牌等级任务相关配置
/// </summary>
public class LiveFansMedalTaskOptions : BaseConfigOptions
{
    public override string SectionName => "LiveFansMedalTaskConfig";

    /// <summary>
    /// 自定义发送弹幕内容，如 "打卡" 等来触发直播间内机器人关键词
    /// </summary>
    public string DanmakuContent { get; set; } = "OvO";

    /// <summary>
    /// 心跳包发送的个数 / 挂机的时间，单位为分钟
    /// </summary>
    public int HeartBeatNumber { get; set; } = 70;

    /// <summary>
    /// 当心跳包发送连续失败多少次时放弃
    /// </summary>
    public int HeartBeatSendGiveUpThreshold { get; set; } = 5;

    /// <summary>
    /// 对于直播时长任务是否跳过粉丝牌等级大于等于 20 的
    /// </summary>
    public bool IsSkipLevel20Medal { get; set; } = true;

    public const int HeartBeatInterval = 60;

    /// <summary>
    /// 点赞次数，默认值为30（用于点亮粉丝勋章）
    /// </summary>
    public int LikeNumber { get; set; } = 30;

    /// <summary>
    /// 发送弹幕次数
    /// </summary>
    public int SendDanmakuNumber { get; set; } = 1;

    /// <summary>
    /// 弹幕发送失败多少次时放弃
    /// </summary>
    public int SendDanmakugiveUpThreshold { get; set; } = 3;

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(DanmakuContent)}", DanmakuContent },
                { $"{SectionName}:{nameof(HeartBeatNumber)}", HeartBeatNumber.ToString() },
                {
                    $"{SectionName}:{nameof(HeartBeatSendGiveUpThreshold)}",
                    HeartBeatSendGiveUpThreshold.ToString()
                },
                {
                    $"{SectionName}:{nameof(IsSkipLevel20Medal)}",
                    IsSkipLevel20Medal.ToString().ToLower()
                },
                { $"{SectionName}:{nameof(LikeNumber)}", LikeNumber.ToString() },
                { $"{SectionName}:{nameof(SendDanmakuNumber)}", SendDanmakuNumber.ToString() },
                {
                    $"{SectionName}:{nameof(SendDanmakugiveUpThreshold)}",
                    SendDanmakugiveUpThreshold.ToString()
                },
            }
        );
    }
}
