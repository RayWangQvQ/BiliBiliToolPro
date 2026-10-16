using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Application.Notifications;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Application.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.Scan(scan =>
            scan.FromAssemblyOf<DailyTaskAppService>()
                .AddClasses(classes => classes.AssignableTo<IAppService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        return services;
    }

    /// <summary>
    /// 注册消息通知服务（本地日志 + Telegram + 自定义Webhook），由 NotificationConfig 控制启停。
    /// </summary>
    public static IServiceCollection AddBiliBiliNotification(this IServiceCollection services)
    {
        services.AddHttpClient(NotificationConstants.HttpClientName);

        // 各通知通道
        services.AddSingleton<SerilogNotificationAdapter>();
        services.AddSingleton<TelegramNotificationService>();
        services.AddSingleton<CustomWebhookNotificationService>();

        // 调度器聚合所有通道，对外暴露为 INotificationService
        services.AddSingleton<CompositeNotificationService>();
        services.AddSingleton<INotificationService>(
            sp => sp.GetRequiredService<CompositeNotificationService>()
        );

        return services;
    }
}
