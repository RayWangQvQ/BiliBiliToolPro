using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Infrastructure.Cookies;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Infrastructure.Extensions;

public static class CookieMonitoringServiceCollectionExtensions
{
    public static IServiceCollection AddCookieMonitoring(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICookieCheckStateStore, FileCookieCheckStateStore>();
        services.AddSingleton<ITaskFailureBatchStateStore, FileTaskFailureBatchStateStore>();
        services.AddSingleton<INotificationService, SerilogNotificationAdapter>();
        return services;
    }
}
