using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LiveLotteryJob(ILogger<LiveLotteryJob> logger, ILiveLotteryTaskAppService appService)
    : BaseJob<LiveLotteryJob>(logger)
{
    private readonly ILogger<LiveLotteryJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(LiveLotteryJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(LiveLotteryJob)} started.");
        await appService.DoTaskAsync();
    }
}
