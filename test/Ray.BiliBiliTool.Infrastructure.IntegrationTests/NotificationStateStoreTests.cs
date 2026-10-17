using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Infrastructure.Cookies;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Xunit;

namespace Ray.BiliBiliTool.Infrastructure.IntegrationTests;

public class NotificationStateStoreTests
{
    [Fact]
    public async Task StateFile_PersistsDeliveryAcrossInstancesWithoutCookieSecrets()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cookie-state-tests-" + Guid.NewGuid());
        var path = Path.Combine(directory, "cookie-check-state.json");
        try
        {
            var config = Config(new() { ["CookieCheck:StateFile"] = path });
            var state = new CookieCheckState(
                "synthetic-hash",
                new DateOnly(2026, 1, 1),
                false,
                new DateOnly(2026, 1, 1)
            );
            await new FileCookieCheckStateStore(config).WriteAsync("123456", state, default);
            Assert.Equal(
                state,
                await new FileCookieCheckStateStore(config).ReadAsync("123456", default)
            );
            var json = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("SESSDATA", json);
            Assert.DoesNotContain("SendKey", json);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PendingBatch_PersistsAcrossInstances_AndCanBeClearedAfterDelivery()
    {
        var directory = Directory.CreateTempSubdirectory("failure-state-test-");
        try
        {
            var path = Path.Combine(directory.FullName, "state.json");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["TaskFailureNotification:StateFile"] = path }
                )
                .Build();
            var store = new FileTaskFailureBatchStateStore(config);
            var state = new TaskFailureBatchState(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                [new("DailyTaskAppService", "***1234", 2)]
            );
            await store.WriteAsync(state, default);
            var restored = await new FileTaskFailureBatchStateStore(config).ReadAsync(default);
            Assert.Equal(2, Assert.Single(restored!.Entries).Count);
            Assert.DoesNotContain("SESSDATA", await File.ReadAllTextAsync(path));
            await store.WriteAsync(null, default);
            Assert.Null(await new FileTaskFailureBatchStateStore(config).ReadAsync(default));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
