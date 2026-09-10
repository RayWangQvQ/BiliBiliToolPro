using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class MangaPrivilegeJob(
    ILogger<MangaPrivilegeJob> logger,
    IMangaPrivilegeTaskAppService appService
) : BaseJob<MangaPrivilegeJob>(logger)
{
    public static readonly JobKey Key = new(nameof(MangaPrivilegeJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
