using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestBiliJob(
    ILogger<TestBiliJob> logger,
    ITestAppService appService,
    ITaskFailureBatchMonitor? failureMonitor = null
) : BaseJob<TestBiliJob>(logger, failureMonitor)
{
    public static readonly JobKey Key = new(nameof(TestBiliJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
