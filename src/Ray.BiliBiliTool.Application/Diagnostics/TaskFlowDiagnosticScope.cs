using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Application.Diagnostics;

public static class TaskFlowDiagnosticScope
{
    private static readonly AsyncLocal<List<Exception>?> HandledFailures = new();

    public static void RecordHandledFailure(Exception? exception)
    {
        if (exception is not null && HandledFailures.Value is { } failures)
        {
            lock (failures)
                failures.Add(exception);
        }
    }

    public static async Task ExecuteAsync(
        ILogger logger,
        string flowName,
        Func<Task> action,
        bool trackHandledFailures = false
    )
    {
        var previous = HandledFailures.Value;
        var failures = trackHandledFailures ? new List<Exception>() : null;
        HandledFailures.Value = failures;
        var flowId = Guid.NewGuid().ToString("N");

        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["FlowName"] = flowName, ["FlowId"] = flowId }
        );

        logger.LogInformation("FlowStart {FlowName}", flowName);

        try
        {
            await action();
            if (failures is { Count: > 0 })
                throw new AggregateException("部分任务执行失败", failures);
            logger.LogInformation("FlowCompleted {FlowName}", flowName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FlowFailed {FlowName}", flowName);
            throw;
        }
        finally
        {
            HandledFailures.Value = previous;
        }
    }
}
