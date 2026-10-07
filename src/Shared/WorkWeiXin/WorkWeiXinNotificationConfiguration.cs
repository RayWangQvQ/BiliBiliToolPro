using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public static class WorkWeiXinNotificationConfiguration
{
    public static IConfiguration WithWorkWeiXinMessageChunking(this IConfiguration configuration)
    {
        var overrides = new Dictionary<string, string?>();
        foreach (var sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
        {
            if (
                string.Equals(sink["Name"], "WorkWeiXinBatched", StringComparison.OrdinalIgnoreCase)
            )
                overrides[$"{sink.Path}:Name"] = "ChunkedWorkWeiXinBatched";
            else if (
                string.Equals(
                    sink["Name"],
                    "WorkWeiXinAppBatched",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                overrides[$"{sink.Path}:Name"] = "VerifiedWorkWeiXinAppBatched";
        }
        if (overrides.Count == 0)
            return configuration;
        int nextIndex =
            configuration
                .GetSection("Serilog:Using")
                .GetChildren()
                .Select(entry => int.TryParse(entry.Key, out int index) ? index : -1)
                .DefaultIfEmpty(-1)
                .Max() + 1;
        overrides[$"Serilog:Using:{nextIndex}"] = typeof(ChunkedWorkWeiXinBatchedSink)
            .Assembly.GetName()
            .Name;
        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(overrides)
            .Build();
    }
}
