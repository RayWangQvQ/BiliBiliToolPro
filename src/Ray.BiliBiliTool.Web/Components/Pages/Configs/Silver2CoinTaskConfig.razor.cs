using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class Silver2CoinTaskConfig : BaseConfigComponent<Silver2CoinTaskOptions>
{
    [Inject]
    private IOptionsMonitor<Silver2CoinTaskOptions> Silver2CoinTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<Silver2CoinTaskOptions> OptionsMonitor =>
        Silver2CoinTaskOptionsMonitor;
}
