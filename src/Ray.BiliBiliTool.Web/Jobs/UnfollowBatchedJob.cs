using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class UnfollowBatchedJob(
    ILogger<UnfollowBatchedJob> logger,
    IUnfollowBatchedTaskAppService appService
) : BaseJob<UnfollowBatchedJob>(logger)
{
    public static readonly JobKey Key = new(nameof(UnfollowBatchedJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
