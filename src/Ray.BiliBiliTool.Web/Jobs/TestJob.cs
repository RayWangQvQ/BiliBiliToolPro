using Quartz;
using Serilog.Context;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestJob(ILogger<TestJob> logger) : BaseJob<TestJob>(logger)
{
    private readonly ILogger<TestJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(TestJob));

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation("TestJob started.");
        for (var i = 0; i < 10; i++)
        {
            _logger.LogInformation($"TestJob: {i}");
            await Task.Delay(5 * 1000);
        }
    }
}
