using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class JobFailureNotificationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task JobExceptions_NotifyOnlyForCronExecutions(bool scheduled)
    {
        var monitor = new CaptureMonitor();
        var service = new FailingDailyTask();
        var job = new DailyJob(NullLogger<DailyJob>.Instance, service, monitor);
        var context = DispatchProxy.Create<IJobExecutionContext, ContextProxy>();
        ((ContextProxy)(object)context).Trigger = scheduled
            ? TriggerBuilder.Create().WithCronSchedule("0 0 8 * * ?").Build()
            : TriggerBuilder.Create().WithSimpleSchedule(s => s.WithRepeatCount(0)).Build();
        await job.Execute(context, default);
        Assert.Equal(!scheduled, service.Suppressed);
        Assert.Equal(scheduled ? 1 : 0, monitor.Batches);
        Assert.Equal(scheduled ? 1 : 0, monitor.Failures);
        Assert.Equal(0, monitor.ActiveBatches);
        Assert.False(TaskFailureNotificationScope.IsSuppressed);
    }

    private sealed class FailingDailyTask : IDailyTaskAppService
    {
        public bool Suppressed { get; private set; }

        public async Task DoTaskAsync(CancellationToken token = default)
        {
            await Task.Yield();
            Suppressed = TaskFailureNotificationScope.IsSuppressed;
            throw new InvalidOperationException("synthetic scheduled failure");
        }
    }

    private sealed class CaptureMonitor : ITaskFailureBatchMonitor
    {
        public int Batches { get; private set; }
        public int ActiveBatches { get; private set; }
        public int Failures { get; private set; }

        public IDisposable BeginBatch()
        {
            Batches++;
            ActiveBatches++;
            return new Lease(this);
        }

        public Task RecordFailureAsync(
            long? userId,
            string taskKey,
            CancellationToken token = default
        )
        {
            Assert.Equal("DailyTaskAppService", taskKey);
            Failures++;
            return Task.CompletedTask;
        }

        public Task FlushReadyAsync(CancellationToken token = default) => Task.CompletedTask;

        private sealed class Lease(CaptureMonitor owner) : IDisposable
        {
            public void Dispose() => owner.ActiveBatches--;
        }
    }

    public class ContextProxy : DispatchProxy
    {
        public ITrigger Trigger { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.Name switch
            {
                "get_Trigger" => Trigger,
                "get_FireInstanceId" => "synthetic-" + Guid.NewGuid().ToString("N"),
                _ => throw new NotSupportedException(method.Name),
            };
    }
}
