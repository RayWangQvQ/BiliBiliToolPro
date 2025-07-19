using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class LiveFansMedalTaskConfig : BaseConfigComponent<LiveFansMedalTaskOptions>
{
    [Inject]
    private IOptionsMonitor<LiveFansMedalTaskOptions> LiveFansMedalTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<LiveFansMedalTaskOptions> OptionsMonitor =>
        LiveFansMedalTaskOptionsMonitor;
}
