using Microsoft.Extensions.DependencyInjection;

namespace Ray.BiliBiliTool.Infrastructure.EF.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddEF(this IServiceCollection services)
    {
        services.AddDbContextFactory<BiliDbContext>();
        services.AddScoped<DbInitializer>();
        return services;
    }
}
