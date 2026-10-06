using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Ray.BiliBiliTool.Web.Extensions;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class DailyServerChanConfigurationTests
{
    [Fact]
    public void DailySummaryDisablesRoundLogPushesAndPreservesStoredNotificationKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Serilog:WriteTo:0:Name"] = "ServerChanBatched",
                    ["Serilog:WriteTo:0:Args:scKey"] = "synthetic-legacy",
                    ["Serilog:WriteTo:0:Args:turboScKey"] = "SCT123synthetic",
                    ["Serilog:WriteTo:1:Name"] = "Console",
                    ["Serilog:WriteTo:1:Args:outputTemplate"] = "synthetic-template",
                }
            )
            .Build();
        var logging = config.WithDailyServerChanNotifications();
        Assert.Equal("", logging["Serilog:WriteTo:0:Args:scKey"]);
        Assert.Equal("", logging["Serilog:WriteTo:0:Args:turboScKey"]);
        Assert.Equal("synthetic-template", logging["Serilog:WriteTo:1:Args:outputTemplate"]);
        Assert.Equal("SCT123synthetic", ServerChanCookieExpiryNotifier.GetSendKey(config));
        Assert.Equal("synthetic-legacy", config["Serilog:WriteTo:0:Args:scKey"]);
    }

    [Fact]
    public void ConfigurationWithoutLegacySinkIsPreserved()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.Same(config, config.WithDailyServerChanNotifications());
    }
}
