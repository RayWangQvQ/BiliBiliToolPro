using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LiveFansMedalJob(ILogger<LiveFansMedalJob> logger, ILiveFansMedalAppService appService)
    : BaseJob<LiveFansMedalJob>(logger)
{
    private readonly ILogger<LiveFansMedalJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(LiveFansMedalJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(LiveFansMedalJob)} started.");
        await appService.DoTaskAsync();
    }
}
