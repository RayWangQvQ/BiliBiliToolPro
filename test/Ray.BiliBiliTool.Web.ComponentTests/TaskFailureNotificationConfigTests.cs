using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Web.Components.Pages.Configs;
using Ray.BiliBiliTool.Web.Services.Pages.Configs;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class TaskFailureNotificationConfigTests : TestContext
{
    [Fact]
    public void PerTaskSwitch_SavesIndependentlyAndShowsSuccessFeedback()
    {
        var workflow = new FakeWorkflow();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<INotificationSettingsWorkflow>(workflow);
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("[data-task-key=DailyTaskAppService] input").Change(false);
        page.Find("button").Click();
        page.WaitForAssertion(() =>
            Assert.Contains("设置已保存", page.Find(".save-result").TextContent)
        );
        Assert.False(workflow.Tasks["DailyTaskAppService"]);
        Assert.True(workflow.Tasks["MangaTaskAppService"]);
        Assert.Contains("设置已保存", page.Find("[role=status]").TextContent);
        Assert.Contains("最终汇总时间", page.Markup);
        Assert.Contains("SendKey 已配置", page.Markup);
    }

    [Fact]
    public void Settings_PersistAcrossReload_WithoutChangingCookieSettingsOrSendKey()
    {
        var directory = Directory.CreateTempSubdirectory("failure-settings-");
        try
        {
            var root = new ConfigurationBuilder()
                .AddSqlite($"Data Source={Path.Combine(directory.FullName, "settings.db")}")
                .Build();
            using var configurationLifetime = root as IDisposable;
            new CookieNotificationSettingsWorkflow(root).Save(false, false, "SCT123synthetic");
            var workflow = new TaskFailureNotificationSettingsWorkflow(root);
            workflow.Save(
                true,
                new Dictionary<string, bool>
                {
                    ["DailyTaskAppService"] = false,
                    ["MangaTaskAppService"] = true,
                }
            );
            root.Reload();
            var restored = new TaskFailureNotificationSettingsWorkflow(root).Read();
            Assert.True(restored.Enabled);
            Assert.False(
                restored.Tasks.Single(task => task.TaskKey == "DailyTaskAppService").Enabled
            );
            Assert.True(
                restored.Tasks.Single(task => task.TaskKey == "MangaTaskAppService").Enabled
            );
            Assert.Equal("SCT123synthetic", root["CookieCheck:ServerChanSendKey"]);
            Assert.False(root.GetValue<bool>("CookieCheck:AutoCheckEnabled"));
            Assert.False(root.GetValue<bool>("CookieCheck:NotifyEnabled"));
            Assert.DoesNotContain("SCT123synthetic", restored.ToString());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void EnableWithoutKey_IsRejected_AndDisabledConfigurationCanBeSaved()
    {
        var directory = Directory.CreateTempSubdirectory("failure-settings-");
        try
        {
            var root = new ConfigurationBuilder()
                .AddSqlite($"Data Source={Path.Combine(directory.FullName, "settings.db")}")
                .Build();
            using var configurationLifetime = root as IDisposable;
            var workflow = new TaskFailureNotificationSettingsWorkflow(root);
            Assert.Throws<InvalidOperationException>(() =>
                workflow.Save(true, new Dictionary<string, bool>())
            );
            workflow.Save(false, new Dictionary<string, bool>());
            Assert.False(workflow.Read().Enabled);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            directory.Delete(recursive: true);
        }
    }

    private sealed class FakeWorkflow : INotificationSettingsWorkflow
    {
        public Dictionary<string, bool> Tasks { get; private set; } = [];

        public NotificationSettings Read() =>
            new(
                new(true, true, true),
                new(
                    true,
                    true,
                    [
                        new("DailyTaskAppService", "每日任务", true),
                        new("MangaTaskAppService", "漫画签到/阅读", true),
                    ]
                )
            );

        public void Save(
            bool autoCheckEnabled,
            bool expiryNotifyEnabled,
            bool failureNotifyEnabled,
            string? newSendKey,
            IReadOnlyDictionary<string, bool> tasks
        ) => Tasks = new(tasks);
    }
}
