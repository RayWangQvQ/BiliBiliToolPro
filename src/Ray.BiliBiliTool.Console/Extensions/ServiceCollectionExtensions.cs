using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;

namespace Ray.BiliBiliTool.Console.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleCoreServices(
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
