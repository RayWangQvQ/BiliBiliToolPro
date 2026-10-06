using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Domain.Exceptions;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TaskRecoveryProgressTests
{
    [Fact]
    public async Task ParallelRecoveriesKeepTheirOwnAccountsAndObservers()
    {
        var first = new List<TaskRecoveryProgress>();
        var second = new List<TaskRecoveryProgress>();
        async Task Run(List<TaskRecoveryProgress> target, long user)
        {
            using var root = new TaskRecoveryProgressScope(target.Add);
            using var step = new TaskRecoveryProgressScope($"{user}/task", user);
            await Task.Yield();
            TaskRecoveryProgressScope.Report(
                "medal",
                "示例牌",
                TaskRecoveryProgressState.Running,
                "心跳已接受",
                60,
                900,
                "秒",
                "每日上限 1/10"
            );
        }
        await Task.WhenAll(Run(first, 1001), Run(second, 1002));
        Assert.Equal(1001, Assert.Single(first).UserId);
        Assert.Equal("1001/task/medal", first[0].Key);
        Assert.Equal(1002, Assert.Single(second).UserId);
        Assert.Equal("1002/task/medal", second[0].Key);
    }

    [Fact]
    public async Task DisposedRootStopsEventsFromInheritedBackgroundWork()
    {
        var entries = new List<TaskRecoveryProgress>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task background;
        using (var root = new TaskRecoveryProgressScope(entries.Add))
        {
            background = Task.Run(async () =>
            {
                await release.Task;
                TaskRecoveryProgressScope.Report(
                    "late",
                    "旧任务",
                    TaskRecoveryProgressState.Running
                );
            });
        }
        release.SetResult();
        await background;
        Assert.Empty(entries);
    }

    [Fact]
    public void ViewerFailureDoesNotInterruptExecutionAndNestedScopeRestoresBatch()
    {
        var entries = new List<TaskRecoveryProgress>();
        using var root = new TaskRecoveryProgressScope(entries.Add);
        using (var step = new TaskRecoveryProgressScope("1001/task", 1001))
        {
            TaskRecoveryProgressScope.Report(
                "task",
                "任务",
                TaskRecoveryProgressState.Failed,
                "错误码 -403"
            );
            Assert.True(step.HasFailures);
        }
        TaskRecoveryProgressScope.Report(
            "batch",
            "本轮补做",
            TaskRecoveryProgressState.Running,
            current: 1,
            total: 2,
            unit: "项"
        );
        Assert.Equal("/batch", entries.Last().Key);
        Assert.Null(entries.Last().UserId);
        using var broken = new TaskRecoveryProgressScope(_ =>
            throw new InvalidOperationException()
        );
        TaskRecoveryProgressScope.Report("task", "任务", TaskRecoveryProgressState.Running);
    }

    [Fact]
    public void FailureDetailsRetainBusinessReasonWithoutCredentialOrRawTransportDetails()
    {
        var message = TaskRecoveryProgressScope.DescribeFailure(
            new BiliBusinessException("错误码 -403，账号异常。SESSDATA=synthetic-secret")
        );
        Assert.Contains("-403", message);
        Assert.Contains("账号异常", message);
        Assert.DoesNotContain("synthetic-secret", message);
        Assert.DoesNotContain(
            "synthetic-private",
            TaskRecoveryProgressScope.DescribeFailure(new HttpRequestException("synthetic-private"))
        );
        Assert.Contains("超时", TaskRecoveryProgressScope.DescribeFailure(new TimeoutException()));
    }
}
