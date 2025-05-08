using Quartz;
using Serilog.Context;

namespace Ray.BiliBiliTool.Web.Jobs;

public class TestJob(ILogger<TestJob> logger) : IJob
{
    public static readonly JobKey Key = new(nameof(TestJob));

    public async Task Execute(IJobExecutionContext context)
    {
        // var correlationId = context.JobDetail.JobDataMap.GetString("CorrelationId");
        var fireInstanceId = context.FireInstanceId;

        // using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("FireInstanceId", fireInstanceId))
        {
            try
            {
                await DoExecuteAsync(context);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }
    }

    private async Task DoExecuteAsync(IJobExecutionContext context)
    {
        logger.LogInformation("TestJob started.");
        for (var i = 0; i < 10; i++)
        {
            logger.LogInformation($"TestJob: {i}");
            await Task.Delay(5 * 1000);
        }
    }
}
