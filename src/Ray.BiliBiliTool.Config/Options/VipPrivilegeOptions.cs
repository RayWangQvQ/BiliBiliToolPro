namespace Ray.BiliBiliTool.Config.Options;

public class VipPrivilegeOptions : IHasCron
{
    public const string SectionName = "VipPrivilegeConfig";

    public string? Cron { get; set; }
    public bool IsEnable { get; set; } = true;

    public Dictionary<string, string> ToConfigDictionary()
    {
        var result = new Dictionary<string, string>
        {
            { $"{SectionName}:{nameof(Cron)}", Cron ?? "" },
            { $"{SectionName}:{nameof(IsEnable)}", IsEnable.ToString().ToLower() },
        };
        return result;
    }
}
