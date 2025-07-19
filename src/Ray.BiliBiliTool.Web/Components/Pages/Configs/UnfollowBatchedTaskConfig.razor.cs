using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class UnfollowBatchedTaskConfig : BaseConfigComponent<UnfollowBatchedTaskOptions>
{
    [Inject]
    private IOptionsMonitor<UnfollowBatchedTaskOptions> UnfollowBatchedTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<UnfollowBatchedTaskOptions> OptionsMonitor =>
        UnfollowBatchedTaskOptionsMonitor;

    protected override async Task OnInitializedAsync()
    {
        // 确保配置对象有默认的GroupName值
        if (string.IsNullOrEmpty(_config.GroupName))
        {
            _config.GroupName = "天选时刻";
        }
        await base.OnInitializedAsync();
    }
}
