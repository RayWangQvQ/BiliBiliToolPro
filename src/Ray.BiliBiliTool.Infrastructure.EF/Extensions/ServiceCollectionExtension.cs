using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Infrastructure.EF.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddEF(this IServiceCollection services)
    {
        services.AddDbContextFactory<BiliDbContext>();
        services.AddScoped<DbInitializer>();
        services.AddScoped<IExecutionLogRepository, ExecutionLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
