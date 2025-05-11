using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class DailyJob(ILogger<DailyJob> logger, IDailyTaskAppService appService)
    : BaseJob<DailyJob>(logger)
{
    private readonly ILogger<DailyJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(DailyJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(DailyJob)} started.");
        await appService.DoTaskAsync();
    }
}
