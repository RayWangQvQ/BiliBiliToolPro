using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestBiliJob(ILogger<TestBiliJob> logger, ITestAppService appService)
    : BaseJob<TestBiliJob>(logger)
{
    public static readonly JobKey Key = new(nameof(TestBiliJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();
}
