namespace Ray.BiliBiliTool.Web.Services;

public interface IScheduledDailyTaskDelay
{
    Task DelayAsync(bool scheduled, CancellationToken token);
}

public sealed class ScheduledDailyTaskDelay(
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<ScheduledDailyTaskDelay> logger
) : IScheduledDailyTaskDelay
{
    public async Task DelayAsync(bool scheduled, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!scheduled)
            return;
        var maximum =
            configuration.GetValue<int?>("DailyTaskConfig:RandomDelayMaxMinutes")
            ?? configuration.GetValue("Security:RandomSleepMaxMin", 10);
        if (maximum <= 0)
            return;
        maximum = Math.Min(maximum, 1440);
        var minutes = Random.Shared.Next(1, maximum + 1);
        logger.LogInformation("每日任务将在 {minutes} 分钟后执行", minutes);
        await Task.Delay(TimeSpan.FromMinutes(minutes), timeProvider, token);
    }
}
