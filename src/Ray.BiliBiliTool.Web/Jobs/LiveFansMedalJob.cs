using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LiveFansMedalJob(
    ILogger<LiveFansMedalJob> logger,
    ILiveFansMedalAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<LiveFansMedalJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(LiveFansMedalJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
