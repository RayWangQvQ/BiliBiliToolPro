using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Application.Diagnostics;

public static class TaskFlowDiagnosticScope
{
    public static async Task ExecuteAsync(ILogger logger, string flowName, Func<Task> action)
    {
        var flowId = Guid.NewGuid().ToString("N");

        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["FlowName"] = flowName, ["FlowId"] = flowId }
        );

        logger.LogInformation("FlowStart {FlowName}", flowName);

        try
        {
            await action();
            logger.LogInformation("FlowCompleted {FlowName}", flowName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FlowFailed {FlowName}", flowName);
            throw;
        }
    }
}
