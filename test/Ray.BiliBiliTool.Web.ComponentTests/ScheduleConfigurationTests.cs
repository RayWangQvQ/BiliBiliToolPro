using BlazingQuartz.Core.Services;
using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
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
        Services.AddSingleton<ILiveMedalParticipationWorkflow>(
            new LiveMedalParticipationWorkflow(_configuration)
        );
        Services.Configure<DailyTaskOptions>(options => options.Cron = "0 0 15 * * ?");
        Services.Configure<ChargeTaskOptions>(options => options.Cron = "0 0 12 28 * ?");
        Services.Configure<LiveFansMedalTaskOptions>(options =>
        {
            options.IsEnable = true;
            options.UseLiveStateMonitoring = false;
            options.FollowDailyTaskLimit = false;
            options.Cron = "0 5 0 * * ?";
        });
        Services.Configure<LiveFansMedalTaskOptions>(
            _configuration.GetSection("LiveFansMedalTaskConfig")
        );
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task LeavingTaskConfigurationKeepsCanceledDraftAndSavesBeforeNavigation()
    {
        var navigation = new UnsavedChangesNavigationTests.TestNavigationManager();
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(navigation);
        var scheduler = await PrepareAsync<DailyJob>(DailyJob.Key, "0 0 15 * * ?");
        try
        {
            var dialogs = RenderComponent<MudDialogProvider>();
            var page = RenderComponent<DailyJobConfig>();
            page.FindComponents<MudSwitch<bool>>().First().Find("input").Change(false);
            Task<bool>? leaving = null;
            await page.InvokeAsync(() =>
            {
                leaving = navigation.NavigateAsync("/Today");
            });
            dialogs.WaitForAssertion(() =>
                Assert.Single(dialogs.FindAll(".unsaved-changes-continue"))
            );
            await dialogs.Find(".unsaved-changes-continue").ClickAsync(new());
            Assert.False(await leaving!);
            Assert.Null(_configuration["DailyTaskConfig:IsEnable"]);
            Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"));
            await page.InvokeAsync(() =>
            {
                leaving = navigation.NavigateAsync("/Today");
            });
            dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
            await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
            Assert.True(await leaving!);
            Assert.Equal("false", _configuration["DailyTaskConfig:IsEnable"]);
            Assert.Equal("http://localhost/Today", navigation.Uri);
            Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
            Assert.Equal(
                TriggerState.Paused,
                await scheduler.GetTriggerState(
                    new($"{DailyJob.Key}.Cron.Trigger", Web.Constants.BiliJobGroup)
                )
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
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
    public async Task AutoMedalModeSavesIntervalPausesCronAndRetainsCustomSettings()
    {
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            using var page = RenderComponent<LiveFansMedalTaskConfig>();
            page.FindComponents<MudBlazor.MudSwitch<bool>>()
                .Single(c => c.Instance.Label == "按主播状态自动执行")
                .Find("input")
                .Change(true);
            Assert.Empty(page.FindAll("select[aria-label=小时]"));
            Assert.DoesNotContain("额度：自动计算", page.Markup);
            var likes = page.FindComponents<MudNumericField<int>>()
                .Single(c => c.Instance.Label == "每个粉丝牌的点赞次数");
            var danmaku = page.FindComponents<MudNumericField<int>>()
                .Single(c => c.Instance.Label == "每个粉丝牌的弹幕次数");
            var watch = page.FindComponents<MudNumericField<int>>()
                .Single(c => c.Instance.Label == "每个粉丝牌的观看时长（分钟）");
            Assert.Equal(300, likes.Instance.Value);
            Assert.Equal(10, danmaku.Instance.Value);
            Assert.Equal(150, watch.Instance.Value);
            Assert.All(
                new[] { likes, danmaku, watch },
                field => Assert.False(field.Instance.Disabled)
            );
            likes.Find("input").Input("180");
            danmaku.Find("input").Input("4");
            watch.Find("input").Input("90");
            page.WaitForAssertion(() =>
                Assert.StartsWith(
                    "每5分钟",
                    System.Text.RegularExpressions.Regex.Replace(
                        page.FindComponent<MudBlazor.MudSelect<int>>()
                            .Find("div.mud-select-input")
                            .TextContent,
                        @"\s",
                        ""
                    )
                )
            );

            await page.InvokeAsync(() =>
                page.FindComponent<MudBlazor.MudSelect<int>>().Instance.ValueChanged.InvokeAsync(2)
            );
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal(
                    "true",
                    _configuration["LiveFansMedalTaskConfig:UseLiveStateMonitoring"]
                )
            );
            page.WaitForAssertion(() =>
                Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"))
            );
            Assert.Equal("2", _configuration["LiveFansMedalTaskConfig:MonitorIntervalMinutes"]);
            Assert.Equal("70", _configuration["LiveFansMedalTaskConfig:HeartBeatNumber"]);
            Assert.Equal("180", _configuration["LiveFansMedalTaskConfig:DailyLikeNumber"]);
            Assert.Equal("4", _configuration["LiveFansMedalTaskConfig:DailyDanmakuNumber"]);
            Assert.Equal("90", _configuration["LiveFansMedalTaskConfig:DailyWatchMinutes"]);
            var key = new TriggerKey(
                $"{LiveFansMedalJob.Key}.Cron.Trigger",
                Web.Constants.BiliJobGroup
            );
            Assert.Equal(TriggerState.Paused, await scheduler.GetTriggerState(key));
            using var reopened = RenderComponent<LiveFansMedalTaskConfig>();
            Assert.Empty(reopened.FindAll("select[aria-label=小时]"));
            Assert.Equal(2, reopened.FindComponent<MudBlazor.MudSelect<int>>().Instance.Value);
            Assert.Equal(
                180,
                reopened
                    .FindComponents<MudNumericField<int>>()
                    .Single(c => c.Instance.Label == "每个粉丝牌的点赞次数")
                    .Instance.Value
            );
            Assert.Equal(
                4,
                reopened
                    .FindComponents<MudNumericField<int>>()
                    .Single(c => c.Instance.Label == "每个粉丝牌的弹幕次数")
                    .Instance.Value
            );
            Assert.Equal(
                90,
                reopened
                    .FindComponents<MudNumericField<int>>()
                    .Single(c => c.Instance.Label == "每个粉丝牌的观看时长（分钟）")
                    .Instance.Value
            );
            reopened.WaitForAssertion(() =>
                Assert.StartsWith(
                    "每2分钟",
                    System.Text.RegularExpressions.Regex.Replace(
                        reopened
                            .FindComponent<MudBlazor.MudSelect<int>>()
                            .Find("div.mud-select-input")
                            .TextContent,
                        @"\s",
                        ""
                    )
                )
            );
            reopened
                .FindComponents<MudBlazor.MudSwitch<bool>>()
                .Single(c => c.Instance.Label == "按主播状态自动执行")
                .Find("input")
                .Change(false);
            Assert.Single(reopened.FindAll("select[aria-label=小时]"));
            Assert.Contains("每个粉丝牌的观看时长（分钟）", reopened.Markup);
            reopened.Find("form").Submit();
            reopened.WaitForAssertion(() =>
                Assert.Equal(
                    "false",
                    _configuration["LiveFansMedalTaskConfig:UseLiveStateMonitoring"]
                )
            );
            reopened.WaitForAssertion(() =>
                Assert.True(reopened.Find(".save-changes-button").HasAttribute("disabled"))
            );
            Assert.Equal(TriggerState.Normal, await scheduler.GetTriggerState(key));
            Assert.Equal(
                "0 5 0 * * ?",
                Assert
                    .IsAssignableFrom<ICronTrigger>(await scheduler.GetTrigger(key))
                    .CronExpressionString
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task StartupPausesDefaultAutoMedalCronAndResumesExplicitTimedMode()
    {
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var startup = new SchedulerConfigurationStartup(
                ((IServiceProvider)Services).GetRequiredService<ISchedulerFactory>(),
                _configuration
            );
            var key = new TriggerKey(
                $"{LiveFansMedalJob.Key}.Cron.Trigger",
                Web.Constants.BiliJobGroup
            );
            await startup.StartingAsync(CancellationToken.None);
            Assert.Equal(TriggerState.Paused, await scheduler.GetTriggerState(key));
            _configuration["LiveFansMedalTaskConfig:UseLiveStateMonitoring"] = "false";
            await startup.StartingAsync(CancellationToken.None);
            Assert.Equal(TriggerState.Normal, await scheduler.GetTriggerState(key));
            _configuration["LiveFansMedalTaskConfig:IsEnable"] = "false";
            await startup.StartingAsync(CancellationToken.None);
            Assert.Equal(TriggerState.Paused, await scheduler.GetTriggerState(key));
        }
        finally
        {
            await scheduler.Shutdown();
        }
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
            var switches = page.FindComponents<MudBlazor.MudSwitch<bool>>();
            switches
                .Single(c => c.Instance.Label == "跟随 B 站每日任务量")
                .Find("input")
                .Change(true);
            switches.Single(c => c.Instance.Label == "点赞").Find("input").Change(false);
            switches.Single(c => c.Instance.Label == "弹幕").Find("input").Change(false);
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
            Assert.Contains("配置已保存", page.Markup);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public void AllTaskPages_EnableSaveOnlyForChangesAndDisableWhenReverted()
    {
        Services.Configure<MangaTaskOptions>(o => o.Cron = "0 0 6 * * ?");
        Services.Configure<MangaPrivilegeTaskOptions>(o => o.Cron = "0 0 6 * * ?");
        Services.Configure<Silver2CoinTaskOptions>(o => o.Cron = "0 0 6 * * ?");
        Services.Configure<VipPrivilegeOptions>(o => o.Cron = "0 0 6 * * ?");
        Services.Configure<VipBigPointOptions>(o => o.Cron = "0 0 6 * * ?");
        Services.Configure<LiveLotteryTaskOptions>(o => o.Cron = "0 0 6 * * ?");
        Services.Configure<UnfollowBatchedTaskOptions>(o => o.Cron = "0 0 6 * * ?");
        Check<DailyJobConfig>();
        Check<ChargeTaskConfig>();
        Check<LiveFansMedalTaskConfig>();
        Check<MangaTaskConfig>();
        Check<MangaPrivilegeTaskConfig>();
        Check<Silver2CoinTaskConfig>();
        Check<VipPrivilegeConfig>();
        Check<VipBigPointConfig>();
        Check<LiveLotteryTaskConfig>();
        Check<UnfollowBatchedTaskConfig>();
        void Check<T>()
            where T : Microsoft.AspNetCore.Components.IComponent
        {
            using var page = RenderComponent<T>();
            Assert.Contains("恢复已保存配置", page.Find(".restore-config-button").TextContent);
            page.Find(".restore-config-button").Click();
            page.WaitForAssertion(() => Assert.Contains("已恢复保存的配置", page.Markup));
            Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
            Assert.Contains("已保存", page.Find(".save-changes-state").TextContent);
            var toggle = page.FindComponents<MudBlazor.MudSwitch<bool>>().First();
            var original = toggle.Instance.Value;
            toggle.Find("input").Change(!original);
            page.WaitForAssertion(() =>
                Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"))
            );
            Assert.Contains("有未保存修改", page.Markup);
            toggle.Find("input").Change(original);
            page.WaitForAssertion(() =>
                Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"))
            );
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DailyTaskRestoreRequiresDecisionAndDoesNotSave(bool confirm)
    {
        var monitor = ((IServiceProvider)Services).GetRequiredService<
            IOptionsMonitor<DailyTaskOptions>
        >();
        var original = monitor.CurrentValue.IsEnable;
        var dialogs = RenderComponent<MudDialogProvider>();
        var page = RenderComponent<DailyJobConfig>();
        page.Find("input[type=checkbox]").Change(!original);
        Assert.Equal(original, monitor.CurrentValue.IsEnable);
        var pending = page.Find(".restore-config-button").ClickAsync(new());
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".restore-config-confirm")));
        Assert.Equal(!original, page.Find("input[type=checkbox]").HasAttribute("checked"));
        Assert.Null(_configuration["DailyTaskConfig:IsEnable"]);
        var preview = System.Environment.GetEnvironmentVariable("RESTORE_CONFIG_PREVIEW");
        if (confirm && !string.IsNullOrEmpty(preview))
            System.IO.File.WriteAllText(preview, page.Markup + dialogs.Markup);
        await dialogs
            .Find(confirm ? ".restore-config-confirm" : ".restore-config-cancel")
            .ClickAsync(new());
        await pending;
        page.WaitForAssertion(() =>
        {
            Assert.Equal(
                confirm ? original : !original,
                page.Find("input[type=checkbox]").HasAttribute("checked")
            );
            Assert.Equal(confirm, page.Find(".save-changes-button").HasAttribute("disabled"));
        });
        Assert.Equal(original, monitor.CurrentValue.IsEnable);
        Assert.Null(_configuration["DailyTaskConfig:IsEnable"]);
        if (confirm)
            Assert.Contains("已恢复保存的配置", page.Markup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OfflineOnlyDanmakuPersistsAndReopensAsUnchanged(bool original)
    {
        _configuration["LiveFansMedalTaskConfig:DanmakuOnlyWhenOffline"] = original.ToString();
        _configuration.Reload();
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var monitor = ((IServiceProvider)Services).GetRequiredService<
                IOptionsMonitor<LiveFansMedalTaskOptions>
            >();
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            page.FindComponents<MudBlazor.MudSwitch<bool>>()
                .Single(c => c.Instance.Label == "仅在主播未开播时发弹幕")
                .Find("input")
                .Change(!original);
            Assert.Equal(original, monitor.CurrentValue.DanmakuOnlyWhenOffline);
            Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"));
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
            {
                Assert.Contains("配置已保存", page.Markup);
                Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
                Assert.Equal(!original, monitor.CurrentValue.DanmakuOnlyWhenOffline);
            });
            page.Dispose();
            var reopened = RenderComponent<LiveFansMedalTaskConfig>();
            Assert.Equal(
                !original,
                reopened
                    .FindComponents<MudBlazor.MudSwitch<bool>>()
                    .Single(c => c.Instance.Label == "仅在主播未开播时发弹幕")
                    .Find("input")
                    .HasAttribute("checked")
            );
            Assert.True(reopened.Find(".save-changes-button").HasAttribute("disabled"));
            var persisted = new ConfigurationBuilder()
                .AddSqlite($"Data Source={Path.Combine(_directory.FullName, "settings.db")}")
                .Build();
            using var persistedLifetime = (IDisposable)persisted;
            Assert.Equal(
                (!original).ToString().ToLowerInvariant(),
                persisted["LiveFansMedalTaskConfig:DanmakuOnlyWhenOffline"]
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task SavingKeepsFormVisibleBlocksDuplicateAndPreservesLaterEdits()
    {
        var service = System.Reflection.DispatchProxy.Create<ISchedulerService, PendingScheduler>();
        var pending = (PendingScheduler)service;
        Services.AddSingleton(service);
        var page = RenderComponent<DailyJobConfig>();
        page.Find("input[type=checkbox]").Change(false);
        var save = page.Find("form").SubmitAsync();
        page.WaitForAssertion(() =>
        {
            Assert.Contains("正在保存", page.Markup);
            Assert.Single(page.FindAll("form"));
            Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
            Assert.Equal(1, pending.Calls);
        });
        await page.Find("form").SubmitAsync();
        Assert.Equal(1, pending.Calls);
        page.Find("input[type=checkbox]").Change(true);
        await page.InvokeAsync(() => pending.Completion.SetResult());
        await save;
        page.WaitForAssertion(() =>
        {
            Assert.Contains("有未保存修改", page.Markup);
            Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"));
        });
        Assert.Equal("false", _configuration["DailyTaskConfig:IsEnable"]);
    }

    [Fact]
    public void FailedSaveKeepsDraftAndAllowsRetryWithoutExposingExceptionDetails()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var page = RenderComponent<DailyJobConfig>();
        page.Find("input[type=checkbox]").Change(false);
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Contains("保存失败，修改已保留，请重试", page.Markup));
        Assert.False(page.Find("input[type=checkbox]").HasAttribute("checked"));
        Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"));
        Assert.DoesNotContain("SQLite configuration provider", page.Markup);
    }

    [Fact]
    public async Task DefaultScheduleNormalizationLeavesSavedDraftUnchanged()
    {
        Services.Configure<DailyTaskOptions>(o => o.Cron = null);
        var scheduler = await PrepareAsync<DailyJob>(DailyJob.Key, "0 0 15 * * ?");
        try
        {
            var page = RenderComponent<DailyJobConfig>();
            page.Find("input[type=checkbox]").Change(false);
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
            {
                Assert.Contains("配置已保存", page.Markup);
                Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
            });
            Assert.Equal(TaskSchedulePlan.DefaultCron, _configuration["DailyTaskConfig:Cron"]);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task ScheduleFailureReportsPersistedSettingsWithoutClaimingCompleteSuccess()
    {
        var service = System.Reflection.DispatchProxy.Create<ISchedulerService, PendingScheduler>();
        var pending = (PendingScheduler)service;
        pending.Completion.SetException(new IOException("synthetic private schedule details"));
        Services.AddSingleton(service);
        var page = RenderComponent<DailyJobConfig>();
        page.Find("input[type=checkbox]").Change(false);
        await page.Find("form").SubmitAsync();
        Assert.Equal("false", _configuration["DailyTaskConfig:IsEnable"]);
        Assert.Contains("配置已保存，执行计划更新失败", page.Markup);
        Assert.DoesNotContain("synthetic private schedule details", page.Markup);
        Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
    }

    public class PendingScheduler : System.Reflection.DispatchProxy
    {
        public int Calls { get; private set; }
        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args)
        {
            if (method!.Name is "PauseTrigger" or "ResumeTrigger")
            {
                Calls++;
                return Completion.Task;
            }
            throw new InvalidOperationException(method.Name);
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
        public Task<LiveMedalSnapshot?> GetCachedAsync(
            int index,
            CancellationToken token = default
        ) => Task.FromResult<LiveMedalSnapshot?>(null);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LiveMedalPage_ExclusionsUpdateRuntimeAndSurviveReload(bool initiallyExcluded)
    {
        Services.AddSingleton<ILiveMedalDashboardService, ExampleMedalDashboard>();
        var original = initiallyExcluded ? "11" : "";
        var expected = initiallyExcluded ? "" : "11";
        _configuration["LiveFansMedalTaskConfig:ExcludedAnchorIds"] = original;
        _configuration.Reload();
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var options = ((IServiceProvider)Services).GetRequiredService<
                IOptionsMonitor<LiveFansMedalTaskOptions>
            >();
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            var dialogs = RenderComponent<MudDialogProvider>();
            const string checkbox = "article[data-anchor='11'] input[aria-label='排除主播 星河']";
            page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
            var order = page.FindAll("article")
                .Select(card => card.GetAttribute("data-anchor"))
                .ToArray();
            var pending = page.Find(checkbox)
                .ChangeAsync(
                    new Microsoft.AspNetCore.Components.ChangeEventArgs
                    {
                        Value = !initiallyExcluded,
                    }
                );
            dialogs.WaitForAssertion(() =>
                Assert.Contains(initiallyExcluded ? "恢复主播参与" : "排除此主播", dialogs.Markup)
            );
            Assert.Equal(
                order,
                page.FindAll("article").Select(card => card.GetAttribute("data-anchor"))
            );
            Assert.Equal(original, options.CurrentValue.ExcludedAnchorIds);
            Assert.Equal(original, _configuration["LiveFansMedalTaskConfig:ExcludedAnchorIds"]);

            dialogs.FindAll("button").Single(button => button.TextContent.Contains("取消")).Click();
            await pending;
            page.WaitForAssertion(() =>
                Assert.Equal(initiallyExcluded, page.Find(checkbox).HasAttribute("checked"))
            );
            pending = page.Find(checkbox)
                .ChangeAsync(
                    new Microsoft.AspNetCore.Components.ChangeEventArgs
                    {
                        Value = !initiallyExcluded,
                    }
                );
            dialogs.WaitForAssertion(() =>
                Assert.Contains(initiallyExcluded ? "恢复参与" : "确定排除", dialogs.Markup)
            );
            dialogs
                .FindAll("button")
                .Single(button =>
                    button.TextContent.Contains(initiallyExcluded ? "恢复参与" : "确定排除")
                )
                .Click();
            await pending;
            page.WaitForAssertion(() =>
            {
                Assert.Contains("设置已保存", page.Markup);
                Assert.Equal(expected, options.CurrentValue.ExcludedAnchorIds);
                Assert.Equal(
                    !initiallyExcluded,
                    options.CurrentValue.GetExcludedAnchorIds().Contains(11)
                );
            });
            Assert.True(
                page.FindAll("button")
                    .Single(button => button.ClassList.Contains("save-changes-button"))
                    .HasAttribute("disabled")
            );

            page.FindAll("button").Single(b => b.TextContent.Contains("恢复已保存配置")).Click();
            page.WaitForAssertion(() =>
                Assert.Equal(!initiallyExcluded, page.Find(checkbox).HasAttribute("checked"))
            );
            page.Dispose();
            var reopened = RenderComponent<LiveFansMedalTaskConfig>();
            reopened.WaitForAssertion(() =>
                Assert.Equal(!initiallyExcluded, reopened.Find(checkbox).HasAttribute("checked"))
            );

            // A separate provider verifies persistence beyond the current circuit.
            var persisted = new ConfigurationBuilder()
                .AddSqlite($"Data Source={Path.Combine(_directory.FullName, "settings.db")}")
                .Build();
            using var persistedLifetime = (IDisposable)persisted;
            Assert.Equal(expected, persisted["LiveFansMedalTaskConfig:ExcludedAnchorIds"]);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task ExclusionSavePreservesOtherDraftsAndLaterFormSaveKeepsLatestList()
    {
        Services.AddSingleton<ILiveMedalDashboardService, ExampleMedalDashboard>();
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            var dialogs = RenderComponent<MudDialogProvider>();
            page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
            var offline = page.FindComponents<MudSwitch<bool>>()
                .Single(c => c.Instance.Label == "仅在主播未开播时发弹幕");
            var original = offline.Instance.Value;
            offline.Find("input").Change(!original);
            var pending = page.Find("article[data-anchor='11'] input[aria-label='排除主播 星河']")
                .ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });
            dialogs.WaitForAssertion(() => Assert.Contains("确定排除", dialogs.Markup));
            dialogs.FindAll("button").Single(b => b.TextContent.Contains("确定排除")).Click();
            await pending;
            Assert.Equal("11", _configuration[LiveMedalParticipationWorkflow.ExclusionKey]);
            Assert.Null(_configuration["LiveFansMedalTaskConfig:DanmakuOnlyWhenOffline"]);
            Assert.Equal(!original, offline.Instance.Value);
            Assert.False(
                page.FindAll("button")
                    .Single(b => b.ClassList.Contains("save-changes-button"))
                    .HasAttribute("disabled")
            );
            await new LiveMedalParticipationWorkflow(_configuration).SetExcludedAsync(22, true);
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal(
                    (!original).ToString().ToLowerInvariant(),
                    _configuration["LiveFansMedalTaskConfig:DanmakuOnlyWhenOffline"]
                )
            );
            Assert.Equal("11,22", _configuration[LiveMedalParticipationWorkflow.ExclusionKey]);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    private sealed class RetryParticipation(ILiveMedalParticipationWorkflow inner)
        : ILiveMedalParticipationWorkflow
    {
        public int Calls;

        public Task<string> SetExcludedAsync(
            long anchorId,
            bool excluded,
            CancellationToken token = default
        ) =>
            ++Calls == 1
                ? Task.FromException<string>(new IOException("synthetic private detail"))
                : inner.SetExcludedAsync(anchorId, excluded, token);
    }

    [Fact]
    public async Task FailedExclusionSaveRestoresSelectionAndAllowsRetry()
    {
        Services.AddSingleton<ILiveMedalDashboardService, ExampleMedalDashboard>();
        var workflow = new RetryParticipation(new LiveMedalParticipationWorkflow(_configuration));
        Services.AddSingleton<ILiveMedalParticipationWorkflow>(workflow);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        var dialogs = RenderComponent<MudDialogProvider>();
        const string selector = "article[data-anchor='11'] input[aria-label='排除主播 星河']";
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
        var order = page.FindAll("article")
            .Select(card => card.GetAttribute("data-anchor"))
            .ToArray();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var pending = page.Find(selector)
                .ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });
            dialogs.WaitForAssertion(() => Assert.Contains("确定排除", dialogs.Markup));
            Assert.True(page.Find(selector).HasAttribute("disabled"));
            dialogs.FindAll("button").Single(b => b.TextContent.Contains("确定排除")).Click();
            await pending;
            if (attempt == 0)
            {
                Assert.Contains("保存失败，请重试", page.Markup);
                Assert.DoesNotContain("synthetic private", page.Markup);
                Assert.False(page.Find(selector).HasAttribute("checked"));
                Assert.Equal(
                    order,
                    page.FindAll("article").Select(card => card.GetAttribute("data-anchor"))
                );
                Assert.Null(_configuration[LiveMedalParticipationWorkflow.ExclusionKey]);
            }
        }
        Assert.Equal(2, workflow.Calls);
        Assert.Equal("11", _configuration[LiveMedalParticipationWorkflow.ExclusionKey]);
        Assert.Equal("11", page.FindAll("article").Last().GetAttribute("data-anchor"));
        Assert.Contains("设置已保存", page.Markup);
        Assert.True(
            page.FindAll("button")
                .Single(b => b.ClassList.Contains("save-changes-button"))
                .HasAttribute("disabled")
        );
    }

    [Fact]
    public async Task LiveMedalPage_WhitelistStaysDraftUntilSavedAndSurvivesReload()
    {
        Services.AddSingleton<ILiveMedalDashboardService, ExampleMedalDashboard>();
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var options = ((IServiceProvider)Services).GetRequiredService<
                IOptionsMonitor<LiveFansMedalTaskOptions>
            >();
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            page.FindComponents<MudBlazor.MudSwitch<bool>>()
                .Single(c => c.Instance.Label == "仅为白名单主播执行任务")
                .Find("input")
                .Change(true);
            page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
            page.Find("article[data-anchor='11'] input[aria-label='选择主播 星河']").Change(true);
            Assert.False(options.CurrentValue.OnlySelectedAnchors);
            Assert.Equal("", options.CurrentValue.IncludedAnchorIds);
            Assert.Null(_configuration["LiveFansMedalTaskConfig:IncludedAnchorIds"]);
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal("11", _configuration["LiveFansMedalTaskConfig:IncludedAnchorIds"])
            );
            Assert.Equal("true", _configuration["LiveFansMedalTaskConfig:OnlySelectedAnchors"]);
            page.WaitForAssertion(() =>
            {
                Assert.True(options.CurrentValue.OnlySelectedAnchors);
                Assert.Equal("11", options.CurrentValue.IncludedAnchorIds);
            });
            page.Dispose();
            var reopened = RenderComponent<LiveFansMedalTaskConfig>();
            reopened.WaitForAssertion(() =>
                Assert.True(
                    reopened
                        .Find("article[data-anchor='11'] input[aria-label='选择主播 星河']")
                        .HasAttribute("checked")
                )
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task LiveMedalPage_PinsStayDraftUntilSavedAndSurviveReopening()
    {
        Services.AddSingleton<ILiveMedalDashboardService, ExampleMedalDashboard>();
        _configuration["LiveFansMedalTaskConfig:PinnedAnchorIds"] = "999";
        _configuration.Reload();
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("article").Count));
            page.Find("article[data-anchor='22'] button.medal-pin").Click();
            Assert.Equal("999", _configuration["LiveFansMedalTaskConfig:PinnedAnchorIds"]);
            Assert.Equal("22", page.Find("article").GetAttribute("data-anchor"));
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
                Assert.Equal("22,999", _configuration["LiveFansMedalTaskConfig:PinnedAnchorIds"])
            );
            page.Dispose();
            var reopened = RenderComponent<LiveFansMedalTaskConfig>();
            reopened.WaitForAssertion(() =>
                Assert.Equal("22", reopened.Find("article").GetAttribute("data-anchor"))
            );
            reopened.Find("article[data-anchor='22'] button.medal-pin").Click();
            reopened.Find("form").Submit();
            reopened.WaitForAssertion(() =>
                Assert.Equal("999", _configuration["LiveFansMedalTaskConfig:PinnedAnchorIds"])
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public async Task WatchWindowSavesSelectableTimesAndReopensWithoutChangingCron()
    {
        var scheduler = await PrepareAsync<LiveFansMedalJob>(LiveFansMedalJob.Key, "0 5 0 * * ?");
        try
        {
            var page = RenderComponent<LiveFansMedalTaskConfig>();
            Assert.Empty(page.FindAll("select[aria-label='开始时间小时']"));
            page.FindComponents<MudSwitch<bool>>()
                .Single(component => component.Instance.Label == "限制每日自动观看时段")
                .Find("input")
                .Change(true);
            page.Find("select[aria-label='开始时间小时']").Change("22");
            page.Find("select[aria-label='开始时间分钟']").Change("30");
            page.Find("select[aria-label='结束时间小时']").Change("2");
            page.Find("select[aria-label='结束时间分钟']").Change("15");
            Assert.Contains("次日", page.Markup);
            var save = page.FindAll("button")
                .Single(button => button.ClassList.Contains("save-changes-button"));
            Assert.False(save.HasAttribute("disabled"));
            page.Find("form").Submit();
            page.WaitForAssertion(() =>
            {
                Assert.Equal("true", _configuration["LiveFansMedalTaskConfig:UseWatchTimeWindow"]);
                Assert.Equal("22:30", _configuration["LiveFansMedalTaskConfig:WatchStartTime"]);
                Assert.Equal("02:15", _configuration["LiveFansMedalTaskConfig:WatchEndTime"]);
                Assert.Equal("0 5 0 * * ?", _configuration["LiveFansMedalTaskConfig:Cron"]);
                Assert.True(
                    page.FindAll("button")
                        .Single(button => button.ClassList.Contains("save-changes-button"))
                        .HasAttribute("disabled")
                );
            });
            var reopened = RenderComponent<LiveFansMedalTaskConfig>();
            reopened.WaitForAssertion(() =>
            {
                Assert.Equal(
                    "22",
                    reopened.Find("select[aria-label='开始时间小时']").GetAttribute("value")
                );
                Assert.Contains("22:30", reopened.Markup);
            });
            var output = System.Environment.GetEnvironmentVariable("WATCH_WINDOW_PREVIEW");
            if (!string.IsNullOrEmpty(output))
                System.IO.File.WriteAllText(output, reopened.Markup);
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Fact]
    public void EqualWatchTimesShowValidationAndKeepDraftUnsaved()
    {
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.FindComponents<MudSwitch<bool>>()
            .Single(component => component.Instance.Label == "限制每日自动观看时段")
            .Find("input")
            .Change(true);
        page.Find("select[aria-label='结束时间小时']").Change("8");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Contains("开始与结束时间需不同", page.Markup));
        Assert.Null(_configuration["LiveFansMedalTaskConfig:UseWatchTimeWindow"]);
        Assert.False(
            page.FindAll("button")
                .Single(button => button.ClassList.Contains("save-changes-button"))
                .HasAttribute("disabled")
        );
    }

    [Fact]
    public async Task LiveMedalPage_ShowsCacheBeforeRefreshAndReplacesItWhenReady()
    {
        var dashboard = new DeferredMedalDashboard();
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() =>
        {
            Assert.Equal(2, page.FindAll("article").Count);
            Assert.Contains("正在更新今日进度", page.Markup);
            Assert.True(dashboard.Refreshed);
        });
        await page.InvokeAsync(() => dashboard.Pending.SetResult(LiveMedalBrowsingTests.Many(3)));
        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll("article").Count));
        Assert.DoesNotContain("正在更新今日进度", page.Markup);
        dashboard.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshing = page.FindAll("button")
            .Single(button => button.TextContent.Contains("刷新进度"))
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        page.WaitForAssertion(() => Assert.Equal(2, dashboard.ReadCalls));
        Assert.Equal(3, page.FindAll("article").Count);
        await page.InvokeAsync(() =>
            dashboard.Pending.SetResult(new([], DateTimeOffset.UtcNow, "请重新登录账号后刷新"))
        );
        await refreshing;
        page.WaitForAssertion(() => Assert.Contains("请重新登录账号后刷新", page.Markup));
        Assert.Equal(3, page.FindAll("article").Count);
        dashboard.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var switching = page.Find("select[aria-label='查看账号']")
            .ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "1" });
        page.WaitForAssertion(() => Assert.Single(page.FindAll("article")));
        Assert.Equal(1, dashboard.LastAccount);
        await page.InvokeAsync(() => dashboard.Pending.SetResult(LiveMedalBrowsingTests.Many(4)));
        await switching;
        page.WaitForAssertion(() => Assert.Equal(4, page.FindAll("article").Count));
        Assert.DoesNotContain("请重新登录账号后刷新", page.Markup);
    }

    private sealed class DeferredMedalDashboard : ILiveMedalDashboardService
    {
        public TaskCompletionSource<LiveMedalSnapshot> Pending { get; set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Refreshed { get; private set; }
        public int LastAccount { get; private set; }
        public int ReadCalls { get; private set; }

        public IReadOnlyList<LiveMedalAccount> GetAccounts() => [new(0, "账号1"), new(1, "账号2")];

        public Task<LiveMedalSnapshot?> GetCachedAsync(
            int index,
            CancellationToken token = default
        ) => Task.FromResult<LiveMedalSnapshot?>(LiveMedalBrowsingTests.Many(index == 0 ? 2 : 1));

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        )
        {
            Refreshed = refresh;
            ReadCalls++;
            LastAccount = index;
            return Pending.Task.WaitAsync(token);
        }
    }

    private sealed class ExampleMedalDashboard : ILiveMedalDashboardService
    {
        public Task<LiveMedalSnapshot?> GetCachedAsync(
            int index,
            CancellationToken token = default
        ) => Task.FromResult<LiveMedalSnapshot?>(null);

        public IReadOnlyList<LiveMedalAccount> GetAccounts() => [new(0, "Example")];

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        ) => Task.FromResult(LiveMedalDashboardTests.Example());
    }
}
