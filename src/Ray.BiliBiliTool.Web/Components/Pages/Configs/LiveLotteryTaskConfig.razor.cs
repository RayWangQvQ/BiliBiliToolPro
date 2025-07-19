using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class LiveLotteryTaskConfig : BaseConfigComponent<LiveLotteryTaskOptions>
{
    [Inject]
    private IOptionsMonitor<LiveLotteryTaskOptions> LiveLotteryTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<LiveLotteryTaskOptions> OptionsMonitor =>
        LiveLotteryTaskOptionsMonitor;
}
