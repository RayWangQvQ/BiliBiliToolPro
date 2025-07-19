using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class VipPrivilegeConfig : BaseConfigComponent<VipPrivilegeOptions>
{
    [Inject]
    private IOptionsMonitor<VipPrivilegeOptions> VipPrivilegeOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<VipPrivilegeOptions> OptionsMonitor =>
        VipPrivilegeOptionsMonitor;
}
