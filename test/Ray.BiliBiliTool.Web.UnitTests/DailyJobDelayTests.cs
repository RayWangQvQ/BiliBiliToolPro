using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services;

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
        var monitor = new RecordingMonitor();
        var context = DispatchProxy.Create<IJobExecutionContext, ContextProxy>();
        ((ContextProxy)context).Trigger = scheduled
            ? TriggerBuilder.Create().WithCronSchedule("0 0 6 * * ?").Build()
            : TriggerBuilder.Create().Build();
        var run = new DailyJob(NullLogger<DailyJob>.Instance, app, monitor, scheduledDelay: delay)
            .Execute(context, CancellationToken.None)
            .AsTask();
        Assert.Equal(scheduled, delay.Scheduled);
        Assert.Equal(scheduled ? 0 : 1, app.Runs);
        Assert.Equal(scheduled ? 1 : 0, monitor.Active);
        delay.Complete();
        await run;
        Assert.Equal(1, app.Runs);
        Assert.Equal(0, monitor.Active);
        Assert.Equal(0, monitor.Failures);
    }

    [Fact]
    public async Task CancelledDelay_DoesNotStartActivities()
    {
        var app = new RecordingApp();
        var monitor = new RecordingMonitor();
        var context = DispatchProxy.Create<IJobExecutionContext, ContextProxy>();
        ((ContextProxy)context).Trigger = TriggerBuilder
            .Create()
            .WithCronSchedule("0 0 6 * * ?")
            .Build();
        using var cancellation = new CancellationTokenSource();
        var run = new DailyJob(
            NullLogger<DailyJob>.Instance,
            app,
            monitor,
            scheduledDelay: new RecordingDelay()
        )
            .Execute(context, cancellation.Token)
            .AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(0, app.Runs);
        Assert.Equal(0, monitor.Active);
        Assert.Equal(0, monitor.Failures);
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

    private sealed class RecordingMonitor : ITaskFailureBatchMonitor
    {
        public int Active { get; private set; }
        public int Failures { get; private set; }

        public IDisposable BeginBatch()
        {
            Active++;
            return new Lease(this);
        }

        public Task RecordFailureAsync(
            long? userId,
            string taskKey,
            CancellationToken token = default
        )
        {
            Failures++;
            return Task.CompletedTask;
        }

        public Task FlushReadyAsync(CancellationToken token = default) => Task.CompletedTask;

        private sealed class Lease(RecordingMonitor monitor) : IDisposable
        {
            public void Dispose() => monitor.Active--;
        }
    }
}
