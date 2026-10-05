using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class ScheduledDailyTaskDelayTests
{
    [Theory]
    [InlineData(true, 1, null, 1)]
    [InlineData(true, 1, 2, 2)]
    [InlineData(true, 2, 0, 0)]
    [InlineData(false, 2, null, 0)]
    public async Task ScheduledDelay_RespectsGlobalOverrideAndManualExecution(
        bool scheduled,
        int global,
        int? local,
        int maximum
    )
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Security:RandomSleepMaxMin"] = global.ToString(),
                    ["DailyTaskConfig:RandomDelayMaxMinutes"] = local?.ToString(),
                }
            )
            .Build();
        var clock = new RecordingClock();
        var delay = new ScheduledDailyTaskDelay(
            config,
            clock,
            NullLogger<ScheduledDailyTaskDelay>.Instance
        );
        var pending = delay.DelayAsync(scheduled, CancellationToken.None);
        if (maximum > 0)
        {
            Assert.InRange(clock.DueTime, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(maximum));
            Assert.False(pending.IsCompleted);
            clock.Fire();
        }
        else
            Assert.Null(clock.Callback);
        await pending;
    }

    [Fact]
    public async Task CancellationDuringDelay_PreventsExecutionFromContinuing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Security:RandomSleepMaxMin"] = "1" }
            )
            .Build();
        var clock = new RecordingClock();
        using var cancellation = new CancellationTokenSource();
        var pending = new ScheduledDailyTaskDelay(
            config,
            clock,
            NullLogger<ScheduledDailyTaskDelay>.Instance
        ).DelayAsync(true, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    private sealed class RecordingClock : TimeProvider
    {
        public TimerCallback? Callback { get; private set; }
        public TimeSpan DueTime { get; private set; }
        private object? _state;

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period
        )
        {
            Callback = callback;
            _state = state;
            DueTime = dueTime;
            return new Timer();
        }

        public void Fire() => Callback!(_state);

        private sealed class Timer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose() { }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
