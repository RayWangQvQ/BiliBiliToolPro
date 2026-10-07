using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Web.Extensions;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class DailyJobSchedulingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CronAndManualExecutionsStartImmediatelyWithLegacyDelaySettings(bool scheduled)
    {
        var app = new RecordingApp();
        var monitor = new RecordingMonitor();
        await using var provider = CreateProvider(app, monitor);
        using var cancellation = new CancellationTokenSource();
        var run = provider
            .GetRequiredService<DailyJob>()
            .Execute(CreateContext(scheduled), cancellation.Token)
            .AsTask();
        try
        {
            Assert.Equal(1, app.Runs);
            Assert.Equal(!scheduled, app.NotificationsSuppressed);
            Assert.Equal(scheduled ? 1 : 0, monitor.Active);
            Assert.False(run.IsCompleted);
        }
        finally
        {
            app.Complete();
            cancellation.Cancel();
            try
            {
                await run;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
        Assert.Equal(0, monitor.Active);
        Assert.Equal(0, monitor.Failures);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanceledExecutionsDoNotStartActivitiesOrRecordFailure(bool scheduled)
    {
        var app = new RecordingApp();
        var monitor = new RecordingMonitor();
        await using var provider = CreateProvider(app, monitor);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider
                .GetRequiredService<DailyJob>()
                .Execute(CreateContext(scheduled), cancellation.Token)
                .AsTask()
        );
        Assert.Equal(0, app.Runs);
        Assert.Equal(0, monitor.Active);
        Assert.Equal(0, monitor.Failures);
    }

    private static ServiceProvider CreateProvider(RecordingApp app, RecordingMonitor monitor)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DailyTaskConfig:RandomDelayMaxMinutes"] = "1440",
                        ["Security:RandomSleepMaxMin"] = "1440",
                    }
                )
                .Build()
        );
        services.AddWebServices();
        services.AddSingleton<IDailyTaskAppService>(app);
        services.AddSingleton<ITaskFailureBatchMonitor>(monitor);
        services.AddTransient<DailyJob>();
        return services.BuildServiceProvider();
    }

    private static IJobExecutionContext CreateContext(bool scheduled)
    {
        var context = DispatchProxy.Create<IJobExecutionContext, ContextProxy>();
        ((ContextProxy)(object)context).Trigger = scheduled
            ? TriggerBuilder.Create().WithCronSchedule("0 0 6 * * ?").Build()
            : TriggerBuilder.Create().WithSimpleSchedule(s => s.WithRepeatCount(0)).Build();
        return context;
    }

    public class ContextProxy : DispatchProxy
    {
        public ITrigger Trigger { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.Name switch
            {
                "get_Trigger" => Trigger,
                "get_FireInstanceId" => "synthetic-schedule-test",
                _ => throw new NotSupportedException(method.Name),
            };
    }

    private sealed class RecordingApp : IDailyTaskAppService
    {
        private readonly TaskCompletionSource _pending = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        public int Runs { get; private set; }
        public bool NotificationsSuppressed { get; private set; }

        public Task DoTaskAsync(CancellationToken token = default)
        {
            Runs++;
            NotificationsSuppressed = TaskFailureNotificationScope.IsSuppressed;
            return _pending.Task;
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
