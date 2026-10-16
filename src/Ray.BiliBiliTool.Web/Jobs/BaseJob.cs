using System.Reflection;
using Quartz;
using Ray.Serilog.Sinks.Batched;
using Serilog.Context;

namespace Ray.BiliBiliTool.Web.Jobs;

public abstract class BaseJob<TJob>(ILogger<TJob> logger) : IJob
    where TJob : BaseJob<TJob>
{
    protected ILogger<TJob> Logger { get; } = logger;

    public async ValueTask Execute(
        IJobExecutionContext context,
        CancellationToken cancellationToken
    )
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
                logger.LogInformation($"{typeof(TJob).Name} started.");
                DateTimeOffset startedAt = DateTimeOffset.Now;

                await DoExecuteAsync(context);

                await SendCompletedNotificationAsync(context, DateTimeOffset.Now - startedAt);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                await SendErrorNotificationAsync(context, e);
            }
            finally
            {
                logger.LogInformation("---");
                logger.LogInformation(
                    "{version} 开源 by {url}",
                    Config.AppVersion.DisplayOf(typeof(Program).Assembly),
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

    /// <summary>
    /// 任务执行成功后的通知钩子，子类按需重写（默认不通知）。
    /// </summary>
    protected virtual Task SendCompletedNotificationAsync(
        IJobExecutionContext context,
        TimeSpan duration
    ) => Task.CompletedTask;

    /// <summary>
    /// 任务执行失败时的通知钩子，子类按需重写（默认不通知）。
    /// </summary>
    protected virtual Task SendErrorNotificationAsync(
        IJobExecutionContext context,
        Exception exception
    ) => Task.CompletedTask;
}
