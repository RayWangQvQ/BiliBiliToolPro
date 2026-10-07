using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Cookies;

namespace Ray.BiliBiliTool.Application.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<ICookieTaskGuard, CookieTaskGuard>();
        services.Scan(scan =>
            scan.FromAssemblyOf<DailyTaskAppService>()
                .AddClasses(classes => classes.AssignableTo<IAppService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        return services;
    }
}
