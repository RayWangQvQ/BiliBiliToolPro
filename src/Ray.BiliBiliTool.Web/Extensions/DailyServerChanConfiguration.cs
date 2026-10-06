namespace Ray.BiliBiliTool.Web.Extensions;

public static class DailyServerChanConfiguration
{
    public static IConfiguration WithDailyServerChanNotifications(this IConfiguration configuration)
    {
        var overrides = new Dictionary<string, string?>();
        foreach (var sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
            if (
                string.Equals(sink["Name"], "ServerChanBatched", StringComparison.OrdinalIgnoreCase)
            )
            {
                overrides[$"{sink.Path}:Args:scKey"] = "";
                overrides[$"{sink.Path}:Args:turboScKey"] = "";
            }
        // Stored keys remain available to the cookie and daily-summary notifiers.
        return overrides.Count == 0
            ? configuration
            : new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddInMemoryCollection(overrides)
                .Build();
    }
}
