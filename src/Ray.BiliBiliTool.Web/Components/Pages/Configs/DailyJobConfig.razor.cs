using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class DailyJobConfig : BaseConfigComponent<DailyTaskOptions>
{
    [Inject]
    private IOptionsMonitor<DailyTaskOptions> DailyTaskOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<DailyTaskOptions> OptionsMonitor => DailyTaskOptionsMonitor;
}
