using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class UnfollowBatchedTaskConfig : BaseConfigComponent<UnfollowBatchedTaskOptions>
{
    [Inject]
    private IOptionsMonitor<UnfollowBatchedTaskOptions> UnfollowBatchedTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<UnfollowBatchedTaskOptions> OptionsMonitor =>
        UnfollowBatchedTaskOptionsMonitor;

    protected override JobKey GetJobKey() => UnfollowBatchedJob.Key;

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
