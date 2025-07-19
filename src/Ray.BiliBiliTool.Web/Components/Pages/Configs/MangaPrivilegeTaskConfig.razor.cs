using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class MangaPrivilegeTaskConfig : BaseConfigComponent<MangaPrivilegeTaskOptions>
{
    [Inject]
    private IOptionsMonitor<MangaPrivilegeTaskOptions> MangaPrivilegeTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<MangaPrivilegeTaskOptions> OptionsMonitor =>
        MangaPrivilegeTaskOptionsMonitor;
}
