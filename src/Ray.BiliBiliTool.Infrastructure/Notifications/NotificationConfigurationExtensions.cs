using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public static class NotificationConfigurationExtensions
{
    public static IConfiguration WithCompatibleHttpNotifications(this IConfiguration configuration)
    {
        var overrides = new Dictionary<string, string?>();
        foreach (var sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
        {
            string? replacement = sink["Name"] switch
            {
                "GotifyBatched" => "CompatibleGotifyBatched",
                "OtherApiBatched" => "HeaderOtherApiBatched",
                _ => null,
            };
            if (replacement is not null)
                overrides[$"{sink.Path}:Name"] = replacement;
        }
        if (overrides.Count == 0)
            return configuration;
        var entries = configuration.GetSection("Serilog:Using").GetChildren();
        int next =
            entries
                .Select(entry => int.TryParse(entry.Key, out int index) ? index : -1)
                .DefaultIfEmpty(-1)
                .Max() + 1;
        overrides[$"Serilog:Using:{next}"] = typeof(HttpNotificationBatchedSink)
            .Assembly.GetName()
            .Name;
        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(overrides)
            .Build();
    }
}
