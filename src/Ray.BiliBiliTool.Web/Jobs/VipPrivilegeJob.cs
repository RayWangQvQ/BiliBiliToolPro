using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class VipPrivilegeJob(
    ILogger<VipPrivilegeJob> logger,
    IVipPrivilegeTaskAppService appService
) : BaseJob<VipPrivilegeJob>(logger)
{
    public static readonly JobKey Key = new(nameof(VipPrivilegeJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
