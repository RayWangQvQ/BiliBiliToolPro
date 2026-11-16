using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public static class TelegramNotificationConfiguration
{
    // Keep stored configuration compatible with the original sink name.
    public static IConfiguration WithTelegramMessageChunking(this IConfiguration configuration)
    {
        var overrides = new Dictionary<string, string?>();
        foreach (var sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
        {
            if (string.Equals(sink["Name"], "TelegramBatched", StringComparison.OrdinalIgnoreCase))
                overrides[$"{sink.Path}:Name"] = "ChunkedTelegramBatched";
        }
        if (overrides.Count == 0)
            return configuration;
        var nextIndex =
            configuration
                .GetSection("Serilog:Using")
                .GetChildren()
                .Select(item => int.TryParse(item.Key, out var index) ? index : -1)
                .DefaultIfEmpty(-1)
                .Max() + 1;
        overrides[$"Serilog:Using:{nextIndex}"] = typeof(ChunkedTelegramBatchedSink)
            .Assembly.GetName()
            .Name;
        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(overrides)
            .Build();
    }
}
