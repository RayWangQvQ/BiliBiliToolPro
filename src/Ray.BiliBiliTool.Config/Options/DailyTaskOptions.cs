namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 程序自定义个性化配置
/// </summary>
public class DailyTaskOptions : BaseConfigOptions
{
    public override string SectionName => "DailyTaskConfig";

    /// <summary>
    /// 是否观看视频
    /// </summary>
    public bool IsWatchVideo { get; set; }

    /// <summary>
    /// 是否分享视频
    /// </summary>
    public bool IsShareVideo { get; set; }

    /// <summary>
    /// 是否开启专栏投币模式
    /// </summary>
    public bool IsDonateCoinForArticle { get; set; }

    /// <summary>
    /// 每日设定的投币数 [0,5]
    /// </summary>
    public int NumberOfCoins { get; set; } = 5;

    /// <summary>
    /// 要保留的硬币数量 [0,int_max]
    /// </summary>
    public int NumberOfProtectedCoins { get; set; } = 0;

    /// <summary>
    /// 达到六级后是否开始白嫖
    /// </summary>
    public bool SaveCoinsWhenLv6 { get; set; } = false;

    /// <summary>
    /// 投币时是否点赞[false,true]
    /// </summary>
    public bool SelectLike { get; set; } = false;

    /// <summary>
    /// 优先选择支持的up主Id集合，配置后会优先从指定的up主下挑选视频进行观看、分享和投币，不配置则从排行耪随机获取支持视频
    /// </summary>
    public string? SupportUpIds { get; set; }

    /// <summary>
    /// 执行客户端操作时的平台 [ios,android]
    /// </summary>
    public string DevicePlatform { get; set; } = "android";

    public List<long> SupportUpIdList
    {
        get
        {
            List<long> re = [];
            if (string.IsNullOrWhiteSpace(SupportUpIds) | SupportUpIds == "-1")
                return re;

            string[] array = SupportUpIds?.Split(',') ?? [];
            foreach (string item in array)
            {
                re.Add(long.TryParse(item.Trim(), out long upId) ? upId : long.MinValue);
            }
            return re;
        }
    }

    private static readonly List<string> DefaultComments =
    [
        "棒",
        "棒唉",
        "棒耶",
        "加油~",
        "UP加油!",
        "支持~",
        "支持支持！",
        "催更啦",
        "顶顶",
        "留下脚印~",
        "干杯",
        "bilibili干杯",
        "o(*￣▽￣*)o",
        "(｡･∀･)ﾉﾞ嗨",
        "(●ˇ∀ˇ●)",
        "( •̀ ω •́ )y",
        "(ง •_•)ง",
        ">.<",
        "^_~",
    ];

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(IsWatchVideo)}", IsWatchVideo.ToString().ToLower() },
                { $"{SectionName}:{nameof(IsShareVideo)}", IsShareVideo.ToString().ToLower() },
                {
                    $"{SectionName}:{nameof(IsDonateCoinForArticle)}",
                    IsDonateCoinForArticle.ToString().ToLower()
                },
                { $"{SectionName}:{nameof(NumberOfCoins)}", NumberOfCoins.ToString() },
                {
                    $"{SectionName}:{nameof(NumberOfProtectedCoins)}",
                    NumberOfProtectedCoins.ToString()
                },
                {
                    $"{SectionName}:{nameof(SaveCoinsWhenLv6)}",
                    SaveCoinsWhenLv6.ToString().ToLower()
                },
                { $"{SectionName}:{nameof(SelectLike)}", SelectLike.ToString().ToLower() },
                { $"{SectionName}:{nameof(SupportUpIds)}", SupportUpIds ?? "" },
                { $"{SectionName}:{nameof(DevicePlatform)}", DevicePlatform },
            }
        );
    }
}
