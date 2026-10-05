using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class DailyJobDelayTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnlyCronRun_WaitsBeforeExecutingActivities(bool scheduled)
    {
        var app = new RecordingApp();
        var delay = new RecordingDelay();
        var context = DispatchProxy.Create<IJobExecutionContext, ContextProxy>();
        ((ContextProxy)context).Trigger = scheduled
            ? TriggerBuilder.Create().WithCronSchedule("0 0 6 * * ?").Build()
            : TriggerBuilder.Create().Build();
        var run = new DailyJob(NullLogger<DailyJob>.Instance, app, delay)
            .Execute(context, CancellationToken.None)
            .AsTask();
        Assert.Equal(scheduled, delay.Scheduled);
        Assert.Equal(scheduled ? 0 : 1, app.Runs);
        delay.Complete();
        await run;
        Assert.Equal(1, app.Runs);
    }

    [Fact]
    public async Task CancelledDelay_DoesNotStartActivities()
    {
        var app = new RecordingApp();
        var context = DispatchProxy.Create<IJobExecutionContext, ContextProxy>();
        ((ContextProxy)context).Trigger = TriggerBuilder
            .Create()
            .WithCronSchedule("0 0 6 * * ?")
            .Build();
        using var cancellation = new CancellationTokenSource();
        var run = new DailyJob(NullLogger<DailyJob>.Instance, app, new RecordingDelay())
            .Execute(context, cancellation.Token)
            .AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(0, app.Runs);
    }

    public class ContextProxy : DispatchProxy
    {
        public ITrigger Trigger { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.Name switch
            {
                "get_Trigger" => Trigger,
                "get_FireInstanceId" => "delay-test",
                _ => throw new NotSupportedException(method.Name),
            };
    }

    private sealed class RecordingApp : IDailyTaskAppService
    {
        public int Runs { get; private set; }

        public Task DoTaskAsync(CancellationToken cancellationToken = default)
        {
            Runs++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDelay : IScheduledDailyTaskDelay
    {
        private readonly TaskCompletionSource _pending = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        public bool Scheduled { get; private set; }

        public Task DelayAsync(bool scheduled, CancellationToken token)
        {
            Scheduled = scheduled;
            return scheduled ? _pending.Task.WaitAsync(token) : Task.CompletedTask;
        }

        public void Complete() => _pending.TrySetResult();
    }
}
