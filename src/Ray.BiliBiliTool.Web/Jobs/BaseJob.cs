using Quartz;
using Serilog.Context;

namespace Ray.BiliBiliTool.Web.Jobs;

public abstract class BaseJob<TJob>(ILogger<TJob> logger) : IJob
    where TJob : BaseJob<TJob>
{
    public async Task Execute(IJobExecutionContext context)
    {
        var fireInstanceId = context.FireInstanceId;
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

    protected abstract Task DoExecuteAsync(IJobExecutionContext context);
}
