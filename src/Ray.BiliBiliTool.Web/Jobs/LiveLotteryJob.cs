using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LiveLotteryJob(ILogger<LiveLotteryJob> logger, ILiveLotteryTaskAppService appService)
    : BaseJob<LiveLotteryJob>(logger)
{
    public static readonly JobKey Key = new(nameof(LiveLotteryJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
