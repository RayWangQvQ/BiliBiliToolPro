using Hangfire;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using System.ComponentModel;
using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Ray.BiliTool.Blazor.Web.Hangfire
{
    public class HangfireHelper
    {
        public static void InitHangfireJobs(IServiceProvider sp)
        {
            //关闭自动重试
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

            GlobalConfiguration.Configuration.UseActivator(new BiliHangfireActivator(sp));

            BackgroundJob.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            RecurringJob.AddOrUpdate<TestService>("Try",
                x => x.Run(),
                Cron.Daily);

            //Test
            RecurringJob.AddOrUpdate<ITestAppService>("Test",
                x => x.DoTaskAsync(new CancellationToken()),
                Cron.Daily);

            /*

            //Daily
            var dailyOptions = sp.GetRequiredService<IOptions<DailyTaskOptions>>().Value;
            string dailyCode = typeof(IDailyTaskAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<IDailyTaskAppService>(dailyCode,
                x => x.DoTaskAsync(new CancellationToken()),
                dailyOptions.Cron);

            //LiveLottery
            var liveLotteryOptions = sp.GetRequiredService<IOptions<LiveLotteryTaskOptions>>().Value;
            string liveLotteryCode = typeof(ILiveLotteryTaskAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<ILiveLotteryTaskAppService>(liveLotteryCode,
                x => x.DoTaskAsync(new CancellationToken()),
                liveLotteryOptions.Cron);

            //LiveFansMedal
            var liveFansMedalOptions = sp.GetRequiredService<IOptions<LiveFansMedalTaskOptions>>().Value;
            string liveFansMedalCode = typeof(ILiveFansMedalAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<ILiveFansMedalAppService>(liveFansMedalCode,
                x => x.DoTaskAsync(new CancellationToken()),
                liveFansMedalOptions.Cron);

            //UnfollowBatched
            var unfollowBatchedOptions = sp.GetRequiredService<IOptions<UnfollowBatchedTaskOptions>>().Value;
            string unfollowBatchedCode = typeof(IUnfollowBatchedTaskAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<IUnfollowBatchedTaskAppService>(unfollowBatchedCode,
                x => x.DoTaskAsync(new CancellationToken()),
                unfollowBatchedOptions.Cron);
            */
        }
    }
}
