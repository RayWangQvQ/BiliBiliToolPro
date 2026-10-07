using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class UnfollowBatchedJob(
    ILogger<UnfollowBatchedJob> logger,
    IUnfollowBatchedTaskAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<UnfollowBatchedJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(UnfollowBatchedJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
