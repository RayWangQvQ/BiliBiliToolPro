using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

public class ConsoleStartupIntegrationTests
{
    [Fact]
    public void Console_startup_binds_configuration_and_registers_critical_services()
    {
        using var host = ConsoleHostBuilder.CreateHost(
            "--ENVIRONMENT=Development",
            "RunTasks=Login",
            "--randomSleep=0"
        );

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        configuration["RunTasks"].Should().Be("Login");

        var securityOptions = host.Services.GetRequiredService<IOptionsMonitor<SecurityOptions>>();
        securityOptions.CurrentValue.RandomSleepMaxMin.Should().Be(0);

        host.Services.GetRequiredService<ILoginTaskAppService>().Should().NotBeNull();
        host.Services.GetRequiredService<IDailyTaskAppService>().Should().NotBeNull();
        host.Services.GetServices<IHostedService>()
            .Should()
            .ContainSingle(x => x is BiliBiliToolHostedService);
    }
}
