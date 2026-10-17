using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class VipBigPointJob(
    ILogger<VipBigPointJob> logger,
    IVipBigPointAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<VipBigPointJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(VipBigPointJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
