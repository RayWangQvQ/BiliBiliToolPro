using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Web.Services.Pages.Configs;
using Xunit;

namespace Ray.BiliBiliTool.Web.IntegrationTests;

public class NotificationSettingsWorkflowTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory(
        "unified-notifications-"
    );
    private readonly IConfigurationRoot _configuration;
    private readonly NotificationSettingsWorkflow _workflow;

    public NotificationSettingsWorkflowTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddSqlite($"Data Source={Path.Combine(_directory.FullName, "settings.db")}")
            .Build();
        _workflow = new(_configuration);
    }

    [Fact]
    public void UnifiedSave_PersistsAllSwitchesAndKey_ThenRetainsKeyWhenInputIsEmpty()
    {
        _workflow.Save(
            false,
            true,
            true,
            "SCT123synthetic",
            new Dictionary<string, bool>
            {
                ["DailyTaskAppService"] = false,
                ["MangaTaskAppService"] = true,
            }
        );
        _configuration.Reload();
        var settings = _workflow.Read();
        Assert.False(settings.Cookie.AutoCheckEnabled);
        Assert.True(settings.Cookie.NotifyEnabled);
        Assert.True(settings.Cookie.HasSendKey);
        Assert.True(settings.TaskFailure.Enabled);
        Assert.Equal(11, settings.TaskFailure.Tasks.Count);
        Assert.Equal(
            11,
            settings.TaskFailure.Tasks.Select(task => task.TaskKey).Distinct().Count()
        );
        Assert.Single(
            settings.TaskFailure.Tasks.Where(task => task.TaskKey == "VipPrivilegeTaskAppService")
        );
        Assert.False(
            settings.TaskFailure.Tasks.Single(task => task.TaskKey == "DailyTaskAppService").Enabled
        );
        Assert.True(
            settings.TaskFailure.Tasks.Single(task => task.TaskKey == "MangaTaskAppService").Enabled
        );
        _workflow.Save(
            true,
            false,
            false,
            "",
            new Dictionary<string, bool> { ["DailyTaskAppService"] = true }
        );
        Assert.Equal("SCT123synthetic", _configuration["CookieCheck:ServerChanSendKey"]);
        Assert.True(_workflow.Read().Cookie.AutoCheckEnabled);
        Assert.False(_workflow.Read().Cookie.NotifyEnabled);
        Assert.False(_workflow.Read().TaskFailure.Enabled);
        Assert.DoesNotContain("SCT123synthetic", _workflow.Read().ToString());
    }

    [Fact]
    public void InvalidSave_LeavesAllExistingSettingsIntact()
    {
        var tasks = new Dictionary<string, bool> { ["DailyTaskAppService"] = true };
        _workflow.Save(true, true, true, "SCT123synthetic", tasks);
        Assert.Throws<InvalidOperationException>(() =>
            _workflow.Save(false, false, false, "invalid", tasks)
        );
        Assert.Throws<InvalidOperationException>(() =>
            _workflow.Save(
                false,
                false,
                false,
                "SCT456replacement",
                new Dictionary<string, bool> { ["UnknownTask"] = true }
            )
        );
        Assert.Equal("SCT123synthetic", _configuration["CookieCheck:ServerChanSendKey"]);
        var restored = _workflow.Read();
        Assert.True(restored.Cookie.AutoCheckEnabled);
        Assert.True(restored.Cookie.NotifyEnabled);
        Assert.True(restored.TaskFailure.Enabled);
    }

    [Fact]
    public void ManualCheckingCanBeSavedWithoutKey_WhenBothRemindersAreOff()
    {
        _workflow.Save(false, false, false, null, new Dictionary<string, bool>());
        Assert.False(_workflow.Read().Cookie.AutoCheckEnabled);
        Assert.False(_workflow.Read().Cookie.HasSendKey);
        Assert.False(_workflow.Read().TaskFailure.Enabled);
    }

    [Fact]
    public void DailySummaryTimePersistsAcrossReloadAndLegacySavePreservesIt()
    {
        Assert.Equal("23:55", _workflow.Read().TaskFailure.DailySummaryTime);
        _workflow.Save(
            true,
            true,
            true,
            "SCT123synthetic",
            new Dictionary<string, bool>(),
            "18:30"
        );
        _configuration.Reload();
        Assert.Equal(
            "18:30",
            new NotificationSettingsWorkflow(_configuration).Read().TaskFailure.DailySummaryTime
        );
        _workflow.Save(false, false, false, "", new Dictionary<string, bool>());
        Assert.Equal("18:30", _workflow.Read().TaskFailure.DailySummaryTime);
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("18:60")]
    [InlineData("invalid")]
    public void InvalidDailySummaryTimeLeavesAllSettingsUnchanged(string value)
    {
        _workflow.Save(
            true,
            true,
            true,
            "SCT123synthetic",
            new Dictionary<string, bool>(),
            "18:30"
        );
        Assert.Throws<InvalidOperationException>(() =>
            _workflow.Save(
                false,
                false,
                false,
                "SCT456synthetic",
                new Dictionary<string, bool>(),
                value
            )
        );
        Assert.Equal("18:30", _workflow.Read().TaskFailure.DailySummaryTime);
        Assert.True(_workflow.Read().TaskFailure.Enabled);
        Assert.True(_workflow.Read().Cookie.AutoCheckEnabled);
        Assert.Equal("SCT123synthetic", _configuration["CookieCheck:ServerChanSendKey"]);
    }

    public void Dispose()
    {
        (_configuration as IDisposable)?.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Delete(recursive: true);
    }
}
