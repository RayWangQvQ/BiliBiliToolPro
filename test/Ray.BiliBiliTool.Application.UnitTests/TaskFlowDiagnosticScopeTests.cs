using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Application.Diagnostics;

namespace Ray.BiliBiliTool.Application.UnitTests;

public class TaskFlowDiagnosticScopeTests
{
    [Fact]
    public async Task ExecuteAsync_HandledStepFailure_IsReportedAfterOtherSteps()
    {
        var failure = new InvalidOperationException("step failed");
        var continued = false;
        var error = await Assert.ThrowsAsync<AggregateException>(() =>
            TaskFlowDiagnosticScope.ExecuteAsync(
                NullLogger.Instance,
                "Manga",
                () =>
                {
                    TaskFlowDiagnosticScope.RecordHandledFailure(failure);
                    continued = true;
                    return Task.CompletedTask;
                },
                trackHandledFailures: true
            )
        );
        Assert.True(continued);
        Assert.Same(failure, Assert.Single(error.InnerExceptions));
        await TaskFlowDiagnosticScope.ExecuteAsync(
            NullLogger.Instance,
            "Next",
            () => Task.CompletedTask,
            trackHandledFailures: true
        );
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentFlows_DoNotShareFailures()
    {
        var failure = new InvalidOperationException("failed");
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = TaskFlowDiagnosticScope.ExecuteAsync(
            NullLogger.Instance,
            "First",
            async () =>
            {
                TaskFlowDiagnosticScope.RecordHandledFailure(failure);
                await ready.Task;
            },
            trackHandledFailures: true
        );
        await TaskFlowDiagnosticScope.ExecuteAsync(
            NullLogger.Instance,
            "Second",
            () => Task.CompletedTask,
            trackHandledFailures: true
        );
        ready.SetResult();
        Assert.Same(
            failure,
            Assert.Single(
                (await Assert.ThrowsAsync<AggregateException>(() => first)).InnerExceptions
            )
        );
    }

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
