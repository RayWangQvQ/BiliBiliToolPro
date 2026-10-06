using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class DailyJob(
    ILogger<DailyJob> logger,
    IDailyTaskAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null,
    IScheduledDailyTaskDelay? scheduledDelay = null
) : BaseJob<DailyJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(DailyJob), Constants.BiliJobGroup);

    protected override Task BeforeExecuteAsync(
        IJobExecutionContext context,
        CancellationToken token
    ) => scheduledDelay?.DelayAsync(context.Trigger is ICronTrigger, token) ?? Task.CompletedTask;

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
