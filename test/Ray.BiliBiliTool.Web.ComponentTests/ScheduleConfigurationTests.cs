using BlazingQuartz.Core.Services;
using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Web.Components.Pages.Configs;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class ScheduleConfigurationTests : TestContext, IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory(
        "schedule-settings-"
    );
    private readonly IConfigurationRoot _configuration;

    public ScheduleConfigurationTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddSqlite($"Data Source={Path.Combine(_directory.FullName, "settings.db")}")
            .Build();
        Services.AddLogging();
        Services.AddMudServices();
        Services.AddSingleton<IConfiguration>(_configuration);
        Services.AddQuartz();
        Services.AddSingleton<ISchedulerService, SchedulerService>();
        Services.AddSingleton<ILiveMedalDashboardService, EmptyMedalDashboard>();
        Services.Configure<DailyTaskOptions>(options => options.Cron = "0 0 15 * * ?");
        Services.Configure<ChargeTaskOptions>(options => options.Cron = "0 0 12 28 * ?");
        Services.Configure<LiveFansMedalTaskOptions>(options =>
        {
            options.IsEnable = true;
            options.Cron = "0 5 0 * * ?";
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private async Task<IScheduler> PrepareAsync<TJob>(JobKey key, string cron)
        where TJob : IJob
    {
        var scheduler = await ((IServiceProvider)Services)
            .GetRequiredService<ISchedulerFactory>()
            .GetScheduler();
        await scheduler.ScheduleJob(
            JobBuilder.Create<TJob>().WithIdentity(key).Build(),
            TriggerBuilder
                .Create()
                .WithIdentity($"{key}.Cron.Trigger", Web.Constants.BiliJobGroup)
                .ForJob(key)
                .WithCronSchedule(cron)
                .Build()
        );
        // The scheduler remains in standby so these tests never execute activities.
        return scheduler;
    }

    [Fact]
    public async Task DailyPage_SavePersistsSelectionAndReschedulesQuartz()
    {
        var scheduler = await PrepareAsync<DailyJob>(DailyJob.Key, "0 0 15 * * ?");
        try
        {
            var page = RenderComponent<DailyJobConfig>();
            page.Find("select[aria-label=小时]").Change("8");
            page.Find("select[aria-label=分钟]").Change("45");
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal("0 45 8 * * ?", _configuration["DailyTaskConfig:Cron"])
            );
            var trigger = await scheduler.GetTrigger(
                new TriggerKey($"{DailyJob.Key}.Cron.Trigger", Web.Constants.BiliJobGroup)
            );
            Assert.Equal(
                "0 45 8 * * ?",
                Assert.IsAssignableFrom<ICronTrigger>(trigger).CronExpressionString
            );
            _configuration.Reload();
            Assert.Equal("0 45 8 * * ?", _configuration["DailyTaskConfig:Cron"]);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task MonthlyPage_SavePersistsDateAndTimeTogether()
    {
        var scheduler = await PrepareAsync<ChargeJob>(ChargeJob.Key, "0 0 12 28 * ?");
        try
        {
            var page = RenderComponent<ChargeTaskConfig>();
            page.Find("select[aria-label=日期]").Change("21");
            page.Find("select[aria-label=小时]").Change("23");
            page.Find("select[aria-label=分钟]").Change("10");
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal("0 10 23 21 * ?", _configuration["ChargeTaskConfig:Cron"])
            );
            var trigger = await scheduler.GetTrigger(
                new TriggerKey($"{ChargeJob.Key}.Cron.Trigger", Web.Constants.BiliJobGroup)
            );
            Assert.Equal(
                "0 10 23 21 * ?",
                Assert.IsAssignableFrom<ICronTrigger>(trigger).CronExpressionString
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task InvalidTime_DoesNotSaveOrReschedule()
    {
        _configuration["DailyTaskConfig:Cron"] = "0 0 15 * * ?";
        var scheduler = await PrepareAsync<DailyJob>(DailyJob.Key, "0 0 15 * * ?");
        try
        {
            var page = RenderComponent<DailyJobConfig>();
            page.Find("select[aria-label=小时]").Change("");
            page.Find("form").Submit();
            Assert.Contains("请选择执行时间", page.Markup);
            Assert.Equal("0 0 15 * * ?", _configuration["DailyTaskConfig:Cron"]);
            var trigger = await scheduler.GetTrigger(
                new TriggerKey($"{DailyJob.Key}.Cron.Trigger", Web.Constants.BiliJobGroup)
            );
            Assert.Equal(
                "0 0 15 * * ?",
                Assert.IsAssignableFrom<ICronTrigger>(trigger).CronExpressionString
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task LiveMedalPage_SavesGoalAndIndependentSwitchesWithSchedule()
    {
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            Assert.DoesNotContain("粉丝牌等级 >= 20", page.Markup);
            Assert.Contains("执行目标", page.Markup);
            var switches = page.FindAll("input[type=checkbox]");
            switches[1].Change(true);
            switches[2].Change(false);
            switches[3].Change(false);
            page.Find("select[aria-label=小时]").Change("21");
            page.Find("select[aria-label=分钟]").Change("15");
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal("true", _configuration["LiveFansMedalTaskConfig:FollowDailyTaskLimit"])
            );
            Assert.Equal("false", _configuration["LiveFansMedalTaskConfig:EnableLike"]);
            Assert.Equal("false", _configuration["LiveFansMedalTaskConfig:EnableDanmaku"]);
            Assert.Equal("true", _configuration["LiveFansMedalTaskConfig:EnableWatch"]);
            Assert.Equal("70", _configuration["LiveFansMedalTaskConfig:HeartBeatNumber"]);
            Assert.Equal("0 15 21 * * ?", _configuration["LiveFansMedalTaskConfig:Cron"]);
            Assert.Contains("Configuration saved successfully", page.Markup);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    public new void Dispose()
    {
        base.Dispose();
        (_configuration as IDisposable)?.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Delete(recursive: true);
    }

    private sealed class EmptyMedalDashboard : ILiveMedalDashboardService
    {
        public IReadOnlyList<LiveMedalAccount> GetAccounts() => [];

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        ) => Task.FromResult(new LiveMedalSnapshot([], DateTimeOffset.UtcNow));
    }

    [Fact]
    public void DailyGuideEditableItemsMatchEnabledFormControls()
    {
        Services.Configure<DailyTaskOptions>(options => options.IsEnable = false);
        var page = RenderComponent<DailyJobConfig>();
        Assert.Contains("开启「启用任务」", page.Find(".task-guide-location").TextContent);
        Assert.Empty(page.FindAll("form .task-setting-help"));
        page.Find("form input[type=checkbox]").Change(true);
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("form .task-setting-help")));
        var form = page.Find("form");
        foreach (
            var item in TaskHelpCatalog
                .Find("DailyTaskAppService")!
                .Features.Where(item => item.SettingKey is not null)
        )
        {
            Assert.Contains(item.Name, form.TextContent);
            Assert.Contains(item.Description, form.TextContent);
        }

        var directory = Environment.GetEnvironmentVariable("MEDAL_PREVIEW_DIR");
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
            var theme = RenderComponent<MudBlazor.MudThemeProvider>();
            File.WriteAllText(
                Path.Combine(directory, "controls.html"),
                "<!doctype html><html lang='zh-CN'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><link rel='stylesheet' href='MudBlazor.min.css'><link rel='stylesheet' href='app.css'></head><body>"
                    + theme.Markup
                    + page.Markup
                    + "</body></html>"
            );
        }
    }

    [Fact]
    public async Task LiveMedalPage_ExclusionsStayDraftUntilSavedAndSurviveReload()
    {
        Services.AddSingleton<ILiveMedalDashboardService, ExampleMedalDashboard>();
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var options = ((IServiceProvider)Services).GetRequiredService<
                IOptionsMonitor<LiveFansMedalTaskOptions>
            >();
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
            page.Find("article[data-anchor='11'] input").Change(true);
            Assert.Equal("", options.CurrentValue.ExcludedAnchorIds);
            Assert.Null(_configuration["LiveFansMedalTaskConfig:ExcludedAnchorIds"]);
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal("11", _configuration["LiveFansMedalTaskConfig:ExcludedAnchorIds"])
            );
            _configuration.Reload();
            Assert.Equal("11", _configuration["LiveFansMedalTaskConfig:ExcludedAnchorIds"]);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    private sealed class ExampleMedalDashboard : ILiveMedalDashboardService
    {
        public IReadOnlyList<LiveMedalAccount> GetAccounts() => [new(0, "Example")];

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        ) => Task.FromResult(LiveMedalDashboardTests.Example());
    }
}
