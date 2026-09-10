using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Web.Auth;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;
using Ray.BiliBiliTool.Web.Services.Pages.Login;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILoginPageStateFactory, LoginPageStateFactory>();
        services.AddScoped<IAdminPageWorkflow, AdminPageWorkflow>();
        services.AddScoped<ISchedulerPageWorkflow, SchedulerPageWorkflow>();
        services.AddScoped<ILogsDialogWorkflow, LogsDialogWorkflow>();
        services.AddScoped<IHistoryDialogWorkflow, HistoryDialogWorkflow>();
        services.AddScoped<IBiliAccountPageWorkflow, BiliAccountPageWorkflow>();

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
            .AddAppServices();
    }
}
