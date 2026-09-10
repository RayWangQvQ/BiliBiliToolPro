using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class MangaJob(ILogger<MangaJob> logger, IMangaTaskAppService appService)
    : BaseJob<MangaJob>(logger)
{
    public static readonly JobKey Key = new(nameof(MangaJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
