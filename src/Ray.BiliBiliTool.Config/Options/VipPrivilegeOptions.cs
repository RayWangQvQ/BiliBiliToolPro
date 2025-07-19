namespace Ray.BiliBiliTool.Config.Options;

public class VipPrivilegeOptions : BaseConfigOptions
{
    public override string SectionName => "VipPrivilegeConfig";

    /// <summary>
    /// 每月几号自动领取会员权益的[-1,31]，-1表示不指定，默认每月1号；0表示不自动领取
    /// </summary>
    public int DayOfReceiveVipPrivilege { get; set; } = 1;

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                {
                    $"{SectionName}:{nameof(DayOfReceiveVipPrivilege)}",
                    DayOfReceiveVipPrivilege.ToString()
                },
            }
        );
    }
}
