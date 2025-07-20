using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class LiveFansMedalTaskConfig : BaseConfigComponent<LiveFansMedalTaskOptions>
{
    [Inject]
    private IOptionsMonitor<LiveFansMedalTaskOptions> LiveFansMedalTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<LiveFansMedalTaskOptions> OptionsMonitor =>
        LiveFansMedalTaskOptionsMonitor;

    protected override JobKey GetJobKey() => LiveFansMedalJob.Key;
}
