namespace Ray.BiliBiliTool.Config.Options;

public class VipPrivilegeOptions : IHasCron
{
    public string? Cron { get; set; }
    public bool IsEnable { get; set; } = true;
}
