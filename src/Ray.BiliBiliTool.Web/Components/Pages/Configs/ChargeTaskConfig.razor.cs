using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class ChargeTaskConfig : BaseConfigComponent<ChargeTaskOptions>
{
    [Inject]
    private IOptionsMonitor<ChargeTaskOptions> ChargeTaskOptionsMonitor { get; set; } = null!;

    protected override IOptionsMonitor<ChargeTaskOptions> OptionsMonitor =>
        ChargeTaskOptionsMonitor;

    private bool _isSpecifyUpToggled;

    protected override Task OnInitializedAsync()
    {
        if (!OptionsMonitor.CurrentValue.AutoChargeUpId.IsNullOrWhiteSpace())
        {
            _isSpecifyUpToggled = true;
        }
        return base.OnInitializedAsync();
    }

    private void OnSpecifyUpToggled(bool isSpecified)
    {
        _isSpecifyUpToggled = !_isSpecifyUpToggled;
        StateHasChanged();
    }

    private Task SetSupportAuthor()
    {
        _config.AutoChargeUpId = "-1";
        return Task.CompletedTask;
    }

    protected override JobKey GetJobKey() => ChargeJob.Key;
}
