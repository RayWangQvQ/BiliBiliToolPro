using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class Silver2CoinJob(ILogger<Silver2CoinJob> logger, ISilver2CoinTaskAppService appService)
    : BaseJob<Silver2CoinJob>(logger)
{
    public static readonly JobKey Key = new(nameof(Silver2CoinJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
