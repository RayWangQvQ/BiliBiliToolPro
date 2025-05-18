using Quartz;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class ServiceCollectionQuartzConfiguratorExtensions
{
    private const string DefaultCron = "0 0 0 1 1 ?";

    public static IServiceCollectionQuartzConfigurator AddBiliJobs(
        this IServiceCollectionQuartzConfigurator quartz,
        IConfiguration configuration
    )
    {
        // Login job
        quartz.AddJob<LoginJob>(opts => opts.WithIdentity(LoginJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(LoginJob.Key)
                .WithIdentity($"{LoginJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(DefaultCron)
        );

        // Daily job
        quartz.AddJob<DailyJob>(opts => opts.WithIdentity(DailyJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(DailyJob.Key)
                .WithIdentity($"{DailyJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(configuration["DailyTaskConfig:Cron"] ?? DefaultCron)
        );

        // Vip big point job
        quartz.AddJob<VipBigPointJob>(opts => opts.WithIdentity(VipBigPointJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(VipBigPointJob.Key)
                .WithIdentity($"{VipBigPointJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(configuration["VipBigPointConfig:Cron"] ?? DefaultCron)
        );

        // Live lottery job
        quartz.AddJob<LiveLotteryJob>(opts => opts.WithIdentity(LiveLotteryJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(LiveLotteryJob.Key)
                .WithIdentity($"{LiveLotteryJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(configuration["LiveLotteryTaskConfig:Cron"] ?? DefaultCron)
        );

        // Live fans medal job
        quartz.AddJob<LiveFansMedalJob>(opts => opts.WithIdentity(LiveFansMedalJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(LiveFansMedalJob.Key)
                .WithIdentity($"{LiveFansMedalJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(configuration["LiveFansMedalTaskConfig:Cron"] ?? DefaultCron)
        );

        // Unfollow batched job
        quartz.AddJob<UnfollowBatchedJob>(opts => opts.WithIdentity(UnfollowBatchedJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(UnfollowBatchedJob.Key)
                .WithIdentity($"{UnfollowBatchedJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(configuration["UnfollowBatchedTaskConfig:Cron"] ?? DefaultCron)
        );

        // Test bili job
        quartz.AddJob<TestBiliJob>(opts => opts.WithIdentity(TestBiliJob.Key));
        quartz.AddTrigger(opts =>
            opts.ForJob(TestBiliJob.Key)
                .WithIdentity($"{TestBiliJob.Key}.Cron.Trigger", Constants.BiliJobGroup)
                .WithCronSchedule(DefaultCron)
        );

        return quartz;
    }
}
