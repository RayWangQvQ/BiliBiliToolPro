namespace Ray.BiliBiliTool.Config.Options;

public class ChargeTaskOptions : BaseConfigOptions
{
    public override string SectionName => "ChargeTaskConfig";

    /// <summary>
    /// 充电Up主Id
    /// </summary>
    public string? AutoChargeUpId { get; set; }

    private string? _chargeComment;

    /// <summary>
    /// 充电后留言
    /// </summary>
    public string ChargeComment
    {
        get =>
            string.IsNullOrWhiteSpace(_chargeComment)
                ? DefaultComments[new Random().Next(0, DefaultComments.Count)]
                : _chargeComment;
        set => _chargeComment = value;
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
                { $"{SectionName}:{nameof(AutoChargeUpId)}", AutoChargeUpId ?? "" },
                { $"{SectionName}:{nameof(ChargeComment)}", _chargeComment ?? "" },
            }
        );
    }
}
