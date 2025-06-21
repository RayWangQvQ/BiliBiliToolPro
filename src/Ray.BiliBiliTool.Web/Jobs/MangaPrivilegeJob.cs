using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class MangaPrivilegeJob(
    ILogger<MangaPrivilegeJob> logger,
    IMangaPrivilegeTaskAppService appService
) : BaseJob<MangaPrivilegeJob>(logger)
{
    private readonly ILogger<MangaPrivilegeJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(MangaPrivilegeJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(MangaPrivilegeJob)} started.");
        await appService.DoTaskAsync();
    }
}
