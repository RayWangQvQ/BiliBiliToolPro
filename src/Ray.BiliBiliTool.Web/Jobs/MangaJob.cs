using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class MangaJob(ILogger<MangaJob> logger, IMangaTaskAppService appService)
    : BaseJob<MangaJob>(logger)
{
    private readonly ILogger<MangaJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(MangaJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(MangaJob)} started.");
        await appService.DoTaskAsync();
    }
}
