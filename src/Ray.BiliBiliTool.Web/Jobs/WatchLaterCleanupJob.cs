using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class WatchLaterCleanupJob(
    ILogger<WatchLaterCleanupJob> logger,
    IWatchLaterCleanupAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<WatchLaterCleanupJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(WatchLaterCleanupJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
