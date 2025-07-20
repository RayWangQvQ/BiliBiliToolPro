using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class DailyJobConfig : BaseConfigComponent<DailyTaskOptions>
{
    [Inject]
    private IOptionsMonitor<DailyTaskOptions> DailyTaskOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<DailyTaskOptions> OptionsMonitor => DailyTaskOptionsMonitor;

    protected override JobKey GetJobKey() => DailyJob.Key;
}
