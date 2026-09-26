using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Host.IntegrationTests.Support;

public sealed class WebHostFactory : WebApplicationFactory<Program>
{
    private static readonly string DatabasePath = Path.Combine(
        Path.GetTempPath(),
        $"bilibili-tool-host-tests-{Guid.NewGuid():N}.db"
    );

    private static readonly string ConnectionString = $"Data Source={DatabasePath};Cache=Shared";

    static WebHostFactory()
    {
        // Program.cs hands builder.Configuration to Quartz, Serilog and the SQLite config provider
        // before Build() merges in ConfigureAppConfiguration sources, so an environment variable is
        // the only way to keep those three off config/BiliBiliTool.db — the developer's real store.
        Environment.SetEnvironmentVariable("ConnectionStrings__Sqlite", ConnectionString);

        // The database belongs to the whole test process, so it can only go when that process ends;
        // deleting it in Dispose would make the next boot re-run every migration.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DeleteDatabase();
    }

    private static void DeleteDatabase()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                File.Delete(DatabasePath + suffix);
            }
            catch (IOException)
            {
                // A still-locked file in the temp directory is litter, not a failed test.
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["RunTasks"] = "Login" }
                );
            }
        );
    }
}
