using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class ChargeJob(ILogger<ChargeJob> logger, IChargeTaskAppService appService)
    : BaseJob<ChargeJob>(logger)
{
    private readonly ILogger<ChargeJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(ChargeJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(ChargeJob)} started.");
        await appService.DoTaskAsync();
    }
}
