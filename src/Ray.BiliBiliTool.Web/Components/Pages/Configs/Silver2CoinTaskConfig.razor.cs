using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class Silver2CoinTaskConfig : BaseConfigComponent<Silver2CoinTaskOptions>
{
    [Inject]
    private IOptionsMonitor<Silver2CoinTaskOptions> Silver2CoinTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<Silver2CoinTaskOptions> OptionsMonitor =>
        Silver2CoinTaskOptionsMonitor;

    protected override JobKey GetJobKey() => Silver2CoinJob.Key;
}
