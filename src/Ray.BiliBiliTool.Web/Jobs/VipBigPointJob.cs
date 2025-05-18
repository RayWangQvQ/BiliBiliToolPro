using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class VipBigPointJob(ILogger<VipBigPointJob> logger, IVipBigPointAppService appService)
    : BaseJob<VipBigPointJob>(logger)
{
    private readonly ILogger<VipBigPointJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(VipBigPointJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(VipBigPointJob)} started.");
        await appService.DoTaskAsync();
    }
}
