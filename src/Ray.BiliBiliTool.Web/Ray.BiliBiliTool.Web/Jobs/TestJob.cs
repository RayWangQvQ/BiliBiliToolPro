using Quartz;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestJob(ILogger<TestJob> logger) : IJob
{
    public static readonly JobKey Key = new("TestJob");

    public async Task Execute(IJobExecutionContext context)
    {
        for (var i = 0; i < 10; i++)
        {
            logger.LogInformation($"TestJob: {i}");
            await Task.Delay(5 * 1000);
        }
    }
}
