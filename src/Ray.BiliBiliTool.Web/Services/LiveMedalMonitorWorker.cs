using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Services;

public sealed class LiveMedalMonitorWorker(
    LiveMedalMonitorCycle cycle,
    IOptionsMonitor<LiveFansMedalTaskOptions> options,
    ILogger<LiveMedalMonitorWorker> logger,
    TimeProvider? clock = null
) : BackgroundService
{
    private readonly Channel<bool> _changes = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest }
    );

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var subscription = options.OnChange((_, _) => _changes.Writer.TryWrite(true));
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await cycle.TickAsync(options.CurrentValue, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogWarning(
                        "Medal monitoring cycle failed: {ErrorType}",
                        error.GetType().Name
                    );
                }
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var duration = TimeSpan.FromMinutes(
                    Math.Clamp(options.CurrentValue.MonitorIntervalMinutes, 1, 30)
                );
                var now = (clock ?? TimeProvider.System).GetUtcNow();
                if (
                    options.CurrentValue.EnableWatch
                    && options.CurrentValue.NextWatchWindowBoundary(now) is { } boundary
                )
                    duration = TimeSpan.FromTicks(
                        Math.Min(duration.Ticks, Math.Max(1, (boundary - now).Ticks))
                    );
                wait.CancelAfter(duration);
                try
                {
                    await _changes.Reader.ReadAsync(wait.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) { }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            await cycle.DisposeAsync();
        }
    }
}
