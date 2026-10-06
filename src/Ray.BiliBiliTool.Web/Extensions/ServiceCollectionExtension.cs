using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure.Extensions;
using Ray.BiliBiliTool.Web.Auth;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;
using Ray.BiliBiliTool.Web.Services.Pages.Configs;
using Ray.BiliBiliTool.Web.Services.Pages.Login;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IScheduledDailyTaskDelay, ScheduledDailyTaskDelay>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILoginPageStateFactory, LoginPageStateFactory>();
        services.AddScoped<IAdminPageWorkflow, AdminPageWorkflow>();
        services.AddScoped<ISchedulerPageWorkflow, SchedulerPageWorkflow>();
        services.AddScoped<ILogsDialogWorkflow, LogsDialogWorkflow>();
        services.AddScoped<IHistoryDialogWorkflow, HistoryDialogWorkflow>();
        services.AddScoped<IBiliAccountPageWorkflow, BiliAccountPageWorkflow>();
        services.AddScoped<
            ICookieNotificationSettingsWorkflow,
            CookieNotificationSettingsWorkflow
        >();
        services.AddScoped<INotificationSettingsWorkflow, NotificationSettingsWorkflow>();
        services.AddMemoryCache();
        services.AddSingleton<ILiveMedalSnapshotStore>(provider => new FileLiveMedalSnapshotStore(
            Path.Combine(
                provider.GetRequiredService<IHostEnvironment>().ContentRootPath,
                "config",
                "live-medal-dashboard-cache"
            ),
            provider.GetRequiredService<ILogger<FileLiveMedalSnapshotStore>>()
        ));
        services.AddScoped<ILiveMedalDashboardService, LiveMedalDashboardService>();
        services.AddScoped<ILiveMedalParticipationWorkflow, LiveMedalParticipationWorkflow>();
        services.AddSingleton<LiveMedalProgressUpdates>();
        services.AddSingleton<Ray.BiliBiliTool.DomainService.ILiveFansMedalProgressObserver>(
            provider => provider.GetRequiredService<LiveMedalProgressUpdates>()
        );
        services.AddSingleton(
            provider => new Ray.BiliBiliTool.DomainService.LiveFansMedalExecutionGate(
                provider.GetRequiredService<TimeProvider>(),
                Path.Combine(
                    provider.GetRequiredService<IHostEnvironment>().ContentRootPath,
                    "config",
                    "live-medal-daily-usage.json"
                ),
                int.TryParse(
                    provider.GetRequiredService<IConfiguration>()[
                        "LiveWatchDiagnostics:ConcurrentRooms"
                    ],
                    out var rooms
                )
                    ? rooms
                    : 8
            )
        );
        services.AddSingleton(provider => new Ray.BiliBiliTool.DomainService.LiveWatchDiagnostics(
            Path.Combine(
                provider.GetRequiredService<IHostEnvironment>().ContentRootPath,
                "config",
                "live-watch-diagnostics"
            ),
            provider.GetRequiredService<TimeProvider>()
        ));
        services.AddSingleton<ILiveMedalMonitorSource, LiveMedalMonitorSource>();
        services.AddSingleton<LiveMedalMonitorCycle>();
        services.AddHostedService<LiveMedalMonitorWorker>();

        // 应用版本：宿主程序集元数据，进程内不变，单例即可
        services.AddSingleton<IAppInfoProvider, AppInfoProvider>();

        // 「今日任务」相关
        services.AddSingleton<ITaskRecordWriter, TaskRecordWriter>();
        services.AddSingleton<ITaskFailureBatchMonitor, TaskFailureBatchMonitor>();
        services.AddSingleton<
            IDailyTaskNotificationStatusSource,
            DailyTaskNotificationStatusSource
        >();
        services.AddHostedService<TaskFailureNotificationWorker>();
        services.AddScoped<
            ITaskFailureNotificationSettingsWorkflow,
            TaskFailureNotificationSettingsWorkflow
        >();
        services.AddSingleton<IBiliAccountProbe, BiliAccountProbe>();
        services.AddScoped<TaskRecoveryExecutor>();
        services.AddScoped<ITodayTaskService, TodayTaskService>();

        return services;
    }

    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddAuthenticationCore();
        services.AddAuthorizationCore();
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "BiliToolWebAuth";
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

        return services;
    }

    public static IServiceCollection AddCoreModuleServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        return services
            .AddBiliBiliConfigs(configuration)
            .AddBiliBiliClientApi(configuration)
            .AddDomainServices()
            .AddCookieMonitoring()
            .AddAppServices();
    }
}
