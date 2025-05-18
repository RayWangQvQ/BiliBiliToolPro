using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestBiliJob(ILogger<TestBiliJob> logger, ITestAppService appService)
    : BaseJob<TestBiliJob>(logger)
{
    private readonly ILogger<TestBiliJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(TestBiliJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(TestBiliJob)} started.");
        await appService.DoTaskAsync();
    }
}
