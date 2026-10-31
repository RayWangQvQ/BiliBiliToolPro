using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class WatchLaterCleanupTaskConfig : BaseConfigComponent<WatchLaterCleanupTaskOptions>
{
    [Inject]
    private IOptionsMonitor<WatchLaterCleanupTaskOptions> WatchLaterCleanupTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<WatchLaterCleanupTaskOptions> OptionsMonitor =>
        WatchLaterCleanupTaskOptionsMonitor;

    protected override JobKey GetJobKey() => WatchLaterCleanupJob.Key;
}
