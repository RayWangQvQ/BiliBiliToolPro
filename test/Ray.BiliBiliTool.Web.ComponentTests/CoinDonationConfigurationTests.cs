using BlazingQuartz.Core.Services;
using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Web.Components.Pages.Configs;
using Ray.BiliBiliTool.Web.Jobs;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class CoinDonationPolicyTests
{
    [Fact]
    public async Task LevelOptions_RenderTheirOwnLabelsAndSaveWithoutLegacyOverride()
    {
        var directory = Directory.CreateTempSubdirectory("coin-level-settings-");
        try
        {
            using var context = new TestContext();
            var config = new ConfigurationBuilder()
                .AddSqlite($"Data Source={Path.Combine(directory.FullName, "settings.db")}")
                .Build();
            using var configLifetime = (IDisposable)config;
            context.Services.AddLogging();
            context.Services.AddMudServices();
            context.Services.AddSingleton<IConfiguration>(config);
            context.Services.AddQuartz();
            context.Services.AddSingleton<ISchedulerService, SchedulerService>();
            context.Services.Configure<DailyTaskOptions>(options =>
            {
                options.IsEnable = true;
                options.SaveCoinsWhenLv6 = true;
                options.Cron = "0 0 6 * * ?";
            });
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            var scheduler = await ((IServiceProvider)context.Services)
                .GetRequiredService<ISchedulerFactory>()
                .GetScheduler();
            await scheduler.ScheduleJob(
                JobBuilder.Create<DailyJob>().WithIdentity(DailyJob.Key).Build(),
                TriggerBuilder
                    .Create()
                    .WithIdentity($"{DailyJob.Key}.Cron.Trigger", Web.Constants.BiliJobGroup)
                    .ForJob(DailyJob.Key)
                    .WithCronSchedule("0 0 6 * * ?")
                    .Build()
            );
            try
            {
                var page = context.RenderComponent<DailyJobConfig>();
                var select = page.FindComponents<MudSelect<int>>()
                    .Single(c => c.Instance.Label == "达到指定等级后停止投币");
                Assert.Equal(6, select.Instance.GetState(x => x.Value));
                var items = page.FindComponents<MudSelectItem<int>>()
                    .GroupBy(c => c.Instance.Value)
                    .Select(g => g.First())
                    .OrderBy(c => c.Instance.Value)
                    .ToList();
                Assert.Equal(Enumerable.Range(0, 7), items.Select(c => c.Instance.Value));
                foreach (var item in items.Where(c => c.Instance.Value > 0))
                    Assert.Contains(
                        $"Lv.{item.Instance.Value} 及以上停止",
                        context.Render(item.Instance.ChildContent!).Markup
                    );
                await page.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(4));
                page.Find("form").Submit();
                page.WaitForAssertion(() =>
                    Assert.Equal("4", config["DailyTaskConfig:CoinDonationStopLevel"])
                );
                Assert.Equal("false", config["DailyTaskConfig:SaveCoinsWhenLv6"]);
                await page.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(0));
                page.Find("form").Submit();
                page.WaitForAssertion(() =>
                    Assert.Equal("0", config["DailyTaskConfig:CoinDonationStopLevel"])
                );
                Assert.Equal("false", config["DailyTaskConfig:SaveCoinsWhenLv6"]);
            }
            finally
            {
                await scheduler.Shutdown();
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            directory.Delete(true);
        }
    }
}
