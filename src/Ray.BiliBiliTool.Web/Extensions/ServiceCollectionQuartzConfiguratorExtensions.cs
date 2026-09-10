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
                storeOptions.UseMicrosoftSQLite(sqlLiteOptions =>
                {
                    sqlLiteOptions.UseDriverDelegate<SQLiteDelegate>();
                    sqlLiteOptions.ConnectionString = sqliteConnStr;
                    sqlLiteOptions.TablePrefix = "QRTZ_";
                });
                storeOptions.UseSystemTextJsonSerializer();
            });

            q.AddBiliJobs(configuration);
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }

    public static IServiceCollectionQuartzConfigurator AddBiliJobs(
        this IServiceCollectionQuartzConfigurator quartz,
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

        // Test bili job
        AddBiliJob<TestBiliJob>(quartz, TestBiliJob.Key, null, configuration);

        return quartz;
    }

    private static void AddBiliJob<TJob>(
        IServiceCollectionQuartzConfigurator quartz,
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
