using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Host.IntegrationTests.Support;

public sealed class WebHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var sqlitePath = Path.Combine(
            Path.GetTempPath(),
            $"bilibili-tool-host-tests-{Guid.NewGuid():N}.db"
        );

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Sqlite"] = $"Data Source={sqlitePath}",
                        ["RunTasks"] = "Login",
                    }
                );
            }
        );
    }
}
