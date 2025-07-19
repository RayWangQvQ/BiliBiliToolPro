using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class VipBigPointConfig : BaseConfigComponent<VipBigPointOptions>
{
    [Inject]
    private IOptionsMonitor<VipBigPointOptions> VipBigPointOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<VipBigPointOptions> OptionsMonitor =>
        VipBigPointOptionsMonitor;
}
