using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

[Collection("Host boot")]
public class SchedulerConfigurationRestartTests
{
    [Fact]
    public async Task SavedDisabledTask_RemainsPausedAcrossRestartAndCanBeEnabledAgain()
    {
        var triggerKey = new TriggerKey(
            $"{ChargeJob.Key}.Cron.Trigger",
            Ray.BiliBiliTool.Web.Constants.BiliJobGroup
        );
        string? original = null;
        using (var first = new WebHostFactory())
        {
            var configuration = first.Services.GetRequiredService<IConfiguration>();
            original = configuration["ChargeTaskConfig:IsEnable"];
            Save(configuration, "false");
        }
        try
        {
            using (var restarted = new WebHostFactory())
            {
                var scheduler = await restarted
                    .Services.GetRequiredService<ISchedulerFactory>()
                    .GetScheduler();
                (await scheduler.GetTriggerState(triggerKey)).Should().Be(TriggerState.Paused);
                Save(restarted.Services.GetRequiredService<IConfiguration>(), "true");
            }
            using var enabled = new WebHostFactory();
            var enabledScheduler = await enabled
                .Services.GetRequiredService<ISchedulerFactory>()
                .GetScheduler();
            (await enabledScheduler.GetTriggerState(triggerKey)).Should().Be(TriggerState.Normal);
        }
        finally
        {
            using var cleanup = new WebHostFactory();
            var config = cleanup.Services.GetRequiredService<IConfiguration>();
            Save(config, original ?? "true");
            await new SchedulerConfigurationStartup(
                cleanup.Services.GetRequiredService<ISchedulerFactory>(),
                config
            ).StartingAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SavedCron_IsUsedByTriggerAfterRestart()
    {
        const string configKey = "ChargeTaskConfig:Cron";
        string? original;
        using (var first = new WebHostFactory())
        {
            var config = (IConfigurationRoot)first.Services.GetRequiredService<IConfiguration>();
            original = config[configKey];
            config
                .Providers.OfType<SqliteConfigurationProvider>()
                .Last()
                .Set(configKey, "0 15 6 * * ?");
            config.Reload();
        }
        try
        {
            using var restarted = new WebHostFactory();
            var scheduler = await restarted
                .Services.GetRequiredService<ISchedulerFactory>()
                .GetScheduler();
            var trigger = await scheduler.GetTrigger(
                new TriggerKey(
                    $"{ChargeJob.Key}.Cron.Trigger",
                    Ray.BiliBiliTool.Web.Constants.BiliJobGroup
                )
            );
            ((ICronTrigger)trigger!).CronExpressionString.Should().Be("0 15 6 * * ?");
        }
        finally
        {
            using var cleanup = new WebHostFactory();
            var config = (IConfigurationRoot)cleanup.Services.GetRequiredService<IConfiguration>();
            config
                .Providers.OfType<SqliteConfigurationProvider>()
                .Last()
                .Set(configKey, original ?? "0 0 0 1 1 ?");
            config.Reload();
        }
    }

    private static void Save(IConfiguration configuration, string value)
    {
        var root = (IConfigurationRoot)configuration;
        root.Providers.OfType<SqliteConfigurationProvider>()
            .Last()
            .Set("ChargeTaskConfig:IsEnable", value);
        root.Reload();
    }
}
