using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Web.Services.Pages.Configs;
using Xunit;

namespace Ray.BiliBiliTool.Web.IntegrationTests;

public class CookieNotificationSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "notification-settings-" + Guid.NewGuid()
    );
    private readonly IConfigurationRoot _configuration;
    private readonly CookieNotificationSettingsWorkflow _workflow;

    public CookieNotificationSettingsTests()
    {
        Directory.CreateDirectory(_directory);
        _configuration = new ConfigurationBuilder()
            .AddSqlite($"Data Source={Path.Combine(_directory, "settings.db")}")
            .Build();
        _workflow = new CookieNotificationSettingsWorkflow(_configuration);
    }

    [Fact]
    public void Save_ValidKeyPersistsWithoutReturningSecret_EmptyInputKeepsKey()
    {
        _workflow.Save(true, true, "SCT123synthetic");
        Assert.True(_workflow.Read().HasSendKey);
        Assert.DoesNotContain("SCT123synthetic", _workflow.Read().ToString());
        _workflow.Save(false, false, "");
        Assert.False(_workflow.Read().NotifyEnabled);
        Assert.False(_workflow.Read().AutoCheckEnabled);
        Assert.Equal("SCT123synthetic", _configuration["CookieCheck:ServerChanSendKey"]);
        _workflow.Save(true, true, "");
        Assert.True(_workflow.Read().NotifyEnabled);
    }

    [Fact]
    public void Save_InvalidKeyDoesNotOverwriteExistingConfiguration()
    {
        _workflow.Save(true, true, "SCT123synthetic");
        Assert.Throws<InvalidOperationException>(() => _workflow.Save(true, true, "invalid-key"));
        Assert.Equal("SCT123synthetic", _configuration["CookieCheck:ServerChanSendKey"]);
    }

    [Fact]
    public void ManualModeCanBeSavedBeforeSendKeyIsConfigured()
    {
        Assert.False(_workflow.Read().NotifyEnabled);
        _workflow.Save(false, false, null);
        Assert.False(_workflow.Read().AutoCheckEnabled);
        Assert.False(_workflow.Read().HasSendKey);
    }

    public void Dispose()
    {
        (_configuration as IDisposable)?.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }
}
