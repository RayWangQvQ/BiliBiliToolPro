using Quartz;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestJob(ILogger<TestJob> logger) : IJob
{
    public static readonly JobKey Key = new("TestJob");

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await DoExcuteAsync(context);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }

    private async Task DoExcuteAsync(IJobExecutionContext context)
    {
        logger.LogInformation("TestJob started.");
        for (var i = 0; i < 10; i++)
        {
            logger.LogInformation($"TestJob: {i}");
            await Task.Delay(5 * 1000);
        }
    }
}
