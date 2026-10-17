using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LiveLotteryJob(
    ILogger<LiveLotteryJob> logger,
    ILiveLotteryTaskAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<LiveLotteryJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(LiveLotteryJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
