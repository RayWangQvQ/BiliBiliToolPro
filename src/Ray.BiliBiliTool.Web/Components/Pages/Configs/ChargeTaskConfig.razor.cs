using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class ChargeTaskConfig : BaseConfigComponent<ChargeTaskOptions>
{
    [Inject]
    private IOptionsMonitor<ChargeTaskOptions> ChargeTaskOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<ChargeTaskOptions> OptionsMonitor =>
        ChargeTaskOptionsMonitor;
}
