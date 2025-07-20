using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class VipPrivilegeConfig : BaseConfigComponent<VipPrivilegeOptions>
{
    [Inject]
    private IOptionsMonitor<VipPrivilegeOptions> VipPrivilegeOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<VipPrivilegeOptions> OptionsMonitor =>
        VipPrivilegeOptionsMonitor;

    protected override JobKey GetJobKey() => VipPrivilegeJob.Key;
}
