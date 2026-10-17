using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class Silver2CoinJob(
    ILogger<Silver2CoinJob> logger,
    ISilver2CoinTaskAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<Silver2CoinJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(Silver2CoinJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
