using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Web.Services.Pages.Configs;

public sealed record TaskFailureNotificationSetting(
    string TaskKey,
    string DisplayName,
    bool Enabled
);

public sealed record TaskFailureNotificationSettings(
    bool Enabled,
    bool HasSendKey,
    IReadOnlyList<TaskFailureNotificationSetting> Tasks,
    string DailySummaryTime = DailyTaskNotificationSchedule.DefaultTime
);

public interface ITaskFailureNotificationSettingsWorkflow
{
    TaskFailureNotificationSettings Read();
    void Save(bool enabled, IReadOnlyDictionary<string, bool> tasks);
}

public sealed class TaskFailureNotificationSettingsWorkflow(IConfiguration configuration)
    : ITaskFailureNotificationSettingsWorkflow
{
    public TaskFailureNotificationSettings Read() =>
        new(
            configuration.GetValue("TaskFailureNotification:Enabled", true),
            ServerChanCookieExpiryNotifier.CreateEndpoint(
                ServerChanCookieExpiryNotifier.GetSendKey(configuration)
            )
                is not null,
            TaskFailureNotificationCatalog
                .All.Select(task => new TaskFailureNotificationSetting(
                    task.TaskKey,
                    task.DisplayName,
                    configuration.GetValue(
                        TaskFailureNotificationCatalog.SettingKey(task.TaskKey),
                        true
                    )
                ))
                .ToArray(),
            DailyTaskNotificationSchedule
                .Parse(configuration[DailyTaskNotificationSchedule.CutoffKey])
                .ToString(@"hh\:mm")
        );

    public void Save(bool enabled, IReadOnlyDictionary<string, bool> tasks)
    {
        if (enabled && !Read().HasSendKey)
            throw new InvalidOperationException("请填写有效的 Server 酱 SendKey");
        if (
            tasks.Keys.Any(key =>
                !TaskFailureNotificationCatalog.All.Any(task => task.TaskKey == key)
            )
        )
            throw new InvalidOperationException("请刷新页面后重新选择任务");
        var root = configuration as IConfigurationRoot;
        var provider =
            root?.Providers.OfType<SqliteConfigurationProvider>().LastOrDefault()
            ?? throw new InvalidOperationException("保存失败，请稍后重试");
        var values = tasks.ToDictionary(
            pair => TaskFailureNotificationCatalog.SettingKey(pair.Key),
            pair => pair.Value.ToString()
        );
        values["TaskFailureNotification:Enabled"] = enabled.ToString();
        provider.BatchSet(values);
        root!.Reload();
    }
}
