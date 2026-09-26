using Quartz;
using Quartz.Impl.AdoJobStore;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class ServiceCollectionQuartzConfiguratorExtensions
{
    // Fires Jan 1 at midnight — disabled by default.
    private const string DefaultCron = "0 0 0 1 1 ?";

    public static IServiceCollection AddBiliScheduler(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var sqliteConnStr =
            configuration.GetConnectionString("Sqlite") ?? throw new InvalidOperationException();

        services.AddQuartz(q =>
        {
            q.UsePersistentStore(storeOptions =>
            {
                storeOptions.UseSqlite(sqliteConnStr);
                storeOptions.UseDriverDelegate<SQLiteDelegate>();
                storeOptions.ConfigureStore(store => store.TablePrefix = "QRTZ_");
                storeOptions.UseSystemTextJsonSerializer();
            });

            q.AddBiliJobs(configuration);
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }

    public static IQuartzBuilder AddBiliJobs(
        this IQuartzBuilder quartz,
        IConfiguration configuration
    )
    {
        // Login job
        AddBiliJob<LoginJob>(quartz, LoginJob.Key, null, configuration);

        // Daily job
        AddBiliJob<DailyJob>(quartz, DailyJob.Key, "DailyTaskConfig:Cron", configuration);

        // Manga job
        AddBiliJob<MangaJob>(quartz, MangaJob.Key, "MangaTaskConfig:Cron", configuration);

        // MangaPrivilege job
        AddBiliJob<MangaPrivilegeJob>(
            quartz,
            MangaPrivilegeJob.Key,
            "MangaPrivilegeTaskConfig:Cron",
            configuration
        );

        // ReceiveVipPrivilege job
        AddBiliJob<VipPrivilegeJob>(
            quartz,
            VipPrivilegeJob.Key,
            "VipPrivilegeConfig:Cron",
            configuration
        );

        // Silver2Coin job
        AddBiliJob<Silver2CoinJob>(
            quartz,
            Silver2CoinJob.Key,
            "Silver2CoinTaskConfig:Cron",
            configuration
        );

        // Charge job
        AddBiliJob<ChargeJob>(quartz, ChargeJob.Key, "ChargeTaskConfig:Cron", configuration);

        // Vip big point job
        AddBiliJob<VipBigPointJob>(
            quartz,
            VipBigPointJob.Key,
            "VipBigPointConfig:Cron",
            configuration
        );

        // Live lottery job
        AddBiliJob<LiveLotteryJob>(
            quartz,
            LiveLotteryJob.Key,
            "LiveLotteryTaskConfig:Cron",
            configuration
        );

        // Live fans medal job
        AddBiliJob<LiveFansMedalJob>(
            quartz,
            LiveFansMedalJob.Key,
            "LiveFansMedalTaskConfig:Cron",
            configuration
        );

        // Unfollow batched job
        AddBiliJob<UnfollowBatchedJob>(
            quartz,
            UnfollowBatchedJob.Key,
            "UnfollowBatchedTaskConfig:Cron",
            configuration
        );

        // 自动补做 job：固定间隔触发（不是 Cron），用于补跑今天漏做的任务
        var autoRecoverInterval = Math.Clamp(
            configuration.GetValue("AutoRecoverConfig:IntervalHours", 2),
            1,
            24
        );

        quartz.AddJob<AutoRecoverJob>(opts => opts.WithIdentity(AutoRecoverJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(AutoRecoverJob.Key)
                .WithIdentity(AutoRecoverJob.TriggerKeyValue)
                .StartAt(DateTimeOffset.UtcNow.AddMinutes(1))
                .WithSimpleSchedule(x =>
                    x.WithInterval(TimeSpan.FromHours(autoRecoverInterval)).RepeatForever()
                )
        );

        // Test bili job
        AddBiliJob<TestBiliJob>(quartz, TestBiliJob.Key, null, configuration);

        return quartz;
    }

    private static void AddBiliJob<TJob>(
        IQuartzBuilder quartz,
        JobKey key,
        string? configCronKey,
        IConfiguration configuration
    )
        where TJob : IJob
    {
        quartz.AddJob<TJob>(opts => opts.WithIdentity(key));
        quartz.AddTrigger(opts =>
            opts.ForJob(key)
                .WithIdentity($"{key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(
                    configCronKey != null
                        ? (configuration[configCronKey] ?? DefaultCron)
                        : DefaultCron
                )
        );
    }
}
