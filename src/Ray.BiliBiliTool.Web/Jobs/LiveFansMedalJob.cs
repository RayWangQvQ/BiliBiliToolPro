using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LiveFansMedalJob(ILogger<LiveFansMedalJob> logger, ILiveFansMedalAppService appService)
    : BaseJob<LiveFansMedalJob>(logger)
{
    public static readonly JobKey Key = new(nameof(LiveFansMedalJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
