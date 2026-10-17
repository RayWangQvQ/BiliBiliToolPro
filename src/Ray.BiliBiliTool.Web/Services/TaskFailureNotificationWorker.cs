namespace Ray.BiliBiliTool.Web.Services;

public sealed class TaskFailureNotificationWorker(ITaskFailureBatchMonitor monitor)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await monitor.FlushReadyAsync(stoppingToken);
    }
}
