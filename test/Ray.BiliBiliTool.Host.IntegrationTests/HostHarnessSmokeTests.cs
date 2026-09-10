using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

public class HostHarnessSmokeTests
{
    [Fact]
    public void Console_host_builder_resolves_critical_application_services()
    {
        using var host = ConsoleHostBuilder.CreateHost(
            "--ENVIRONMENT=Development",
            "RunTasks=Login"
        );

        host.Services.GetRequiredService<ILoginTaskAppService>().Should().NotBeNull();
        host.Services.GetRequiredService<IDailyTaskAppService>().Should().NotBeNull();
    }

    [Fact]
    public void Web_host_factory_can_be_constructed()
    {
        using var factory = new WebHostFactory();

        factory.Should().NotBeNull();
    }
}
