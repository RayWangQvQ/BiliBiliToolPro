using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Application.Diagnostics;

namespace Ray.BiliBiliTool.Application.UnitTests;

public class TaskFlowDiagnosticScopeTests
{
    [Fact]
    public async Task ExecuteAsync_ActionSucceeds_InvokesExactlyOnce()
    {
        var count = 0;

        await TaskFlowDiagnosticScope.ExecuteAsync(
            NullLogger.Instance,
            "DailyTask",
            () =>
            {
                count++;
                return Task.CompletedTask;
            }
        );

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ExecuteAsync_ActionFails_PropagatesOriginalException()
    {
        var failure = new InvalidOperationException("failed");

        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TaskFlowDiagnosticScope.ExecuteAsync(
                NullLogger.Instance,
                "DailyTask",
                () => Task.FromException(failure)
            )
        );

        Assert.Same(failure, observed);
    }
}
