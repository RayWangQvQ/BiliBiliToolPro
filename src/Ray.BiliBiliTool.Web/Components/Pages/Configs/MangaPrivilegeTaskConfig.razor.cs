using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class MangaPrivilegeTaskConfig : BaseConfigComponent<MangaPrivilegeTaskOptions>
{
    [Inject]
    private IOptionsMonitor<MangaPrivilegeTaskOptions> MangaPrivilegeTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<MangaPrivilegeTaskOptions> OptionsMonitor =>
        MangaPrivilegeTaskOptionsMonitor;

    protected override JobKey GetJobKey() => MangaPrivilegeJob.Key;
}
