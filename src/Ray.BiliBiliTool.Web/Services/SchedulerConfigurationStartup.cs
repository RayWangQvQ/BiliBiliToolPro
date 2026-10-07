using Quartz;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Services;

public sealed class SchedulerConfigurationStartup(
    ISchedulerFactory factory,
    IConfiguration configuration
) : IHostedLifecycleService
{
    public static IReadOnlyDictionary<JobKey, string> ConfiguredJobs { get; } =
        new Dictionary<JobKey, string>
        {
            [DailyJob.Key] = "DailyTaskConfig",
            [MangaJob.Key] = "MangaTaskConfig",
            [MangaPrivilegeJob.Key] = "MangaPrivilegeTaskConfig",
            [VipPrivilegeJob.Key] = "VipPrivilegeConfig",
            [Silver2CoinJob.Key] = "Silver2CoinTaskConfig",
            [ChargeJob.Key] = "ChargeTaskConfig",
            [VipBigPointJob.Key] = "VipBigPointConfig",
            [LiveLotteryJob.Key] = "LiveLotteryTaskConfig",
            [LiveFansMedalJob.Key] = "LiveFansMedalTaskConfig",
            [UnfollowBatchedJob.Key] = "UnfollowBatchedTaskConfig",
        };

    // All lifecycle StartingAsync hooks finish before Quartz starts firing jobs.
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        var scheduler = await factory.GetScheduler(cancellationToken);
        foreach (var (jobKey, section) in ConfiguredJobs)
        {
            var triggerKey = new TriggerKey($"{jobKey}.Cron.Trigger", Constants.BiliJobGroup);
            if (await scheduler.GetTrigger(triggerKey, cancellationToken) is null)
                continue;
            if (configuration.GetValue($"{section}:IsEnable", true))
                await scheduler.ResumeTrigger(triggerKey, cancellationToken);
            else
                await scheduler.PauseTrigger(triggerKey, cancellationToken);
        }
    }

    public Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken token) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken token) => Task.CompletedTask;

    public Task StopAsync(CancellationToken token) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken token) => Task.CompletedTask;
}
