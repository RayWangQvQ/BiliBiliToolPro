using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class ReceiveVipPrivilegeJob(
    ILogger<ReceiveVipPrivilegeJob> logger,
    IDailyTaskAppService appService
) : BaseJob<ReceiveVipPrivilegeJob>(logger)
{
    private readonly ILogger<ReceiveVipPrivilegeJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(ReceiveVipPrivilegeJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(ReceiveVipPrivilegeJob)} started.");
        await appService.DoTaskAsync();
    }
}
