using System.Reflection;
using Quartz;
using Ray.Serilog.Sinks.Batched;
using Serilog.Context;

namespace Ray.BiliBiliTool.Web.Jobs;

public abstract class BaseJob<TJob>(ILogger<TJob> logger) : IJob
    where TJob : BaseJob<TJob>
{
    public async Task Execute(IJobExecutionContext context)
    {
        var fireInstanceId = context.FireInstanceId;

        using (LogContext.PushProperty("FireInstanceId", fireInstanceId))
        using (
            LogContext.PushProperty(
                Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey,
                fireInstanceId
            )
        )
        {
            try
            {
                await DoExecuteAsync(context);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
            finally
            {
                logger.LogInformation("---");
                logger.LogInformation(
                    "v{version} 开源 by {url}",
                    typeof(Program).Assembly.GetName().Version?.ToString(),
                    Config.Constants.SourceCodeUrl + Environment.NewLine
                );
            }
        }

        try
        {
            await BatchSinkManager.FlushAsync(fireInstanceId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fail to push logs");
        }
    }

    protected abstract Task DoExecuteAsync(IJobExecutionContext context);
}
