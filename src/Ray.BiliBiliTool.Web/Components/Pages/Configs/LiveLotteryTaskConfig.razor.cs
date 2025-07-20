using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class LiveLotteryTaskConfig : BaseConfigComponent<LiveLotteryTaskOptions>
{
    [Inject]
    private IOptionsMonitor<LiveLotteryTaskOptions> LiveLotteryTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<LiveLotteryTaskOptions> OptionsMonitor =>
        LiveLotteryTaskOptionsMonitor;

    protected override JobKey GetJobKey() => LiveLotteryJob.Key;
}
