using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class MangaPrivilegeJob(
    ILogger<MangaPrivilegeJob> logger,
    IMangaPrivilegeTaskAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<MangaPrivilegeJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(MangaPrivilegeJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
