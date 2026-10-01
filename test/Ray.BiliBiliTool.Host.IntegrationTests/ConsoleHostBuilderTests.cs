using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

public class ConsoleHostBuilderTests
{
    [Fact]
    public void CreateHost_ValidArguments_ResolvesCriticalApplicationServices()
    {
        using var host = ConsoleHostBuilder.CreateHost(
            "--ENVIRONMENT=Development",
            "RunTasks=Login"
        );

        host.Services.GetRequiredService<ILoginTaskAppService>().Should().NotBeNull();
        host.Services.GetRequiredService<IDailyTaskAppService>().Should().NotBeNull();
    }
}
