using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class MangaTaskConfig : BaseConfigComponent<MangaTaskOptions>
{
    [Inject]
    private IOptionsMonitor<MangaTaskOptions> MangaTaskOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<MangaTaskOptions> OptionsMonitor => MangaTaskOptionsMonitor;

    protected override JobKey GetJobKey() => MangaJob.Key;
}
