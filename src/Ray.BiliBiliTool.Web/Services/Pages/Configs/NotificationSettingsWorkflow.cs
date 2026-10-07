using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Web.Services.Pages.Configs;

public sealed record NotificationSettings(
    CookieNotificationSettings Cookie,
    TaskFailureNotificationSettings TaskFailure
);

public interface INotificationSettingsWorkflow
{
    NotificationSettings Read();
    void Save(
        bool autoCheckEnabled,
        bool expiryNotifyEnabled,
        bool failureNotifyEnabled,
        string? newSendKey,
        IReadOnlyDictionary<string, bool> tasks
    );
}

public sealed class NotificationSettingsWorkflow(IConfiguration configuration)
    : INotificationSettingsWorkflow
{
    public NotificationSettings Read() =>
        new(
            new CookieNotificationSettingsWorkflow(configuration).Read(),
            new TaskFailureNotificationSettingsWorkflow(configuration).Read()
        );

    public void Save(
        bool autoCheckEnabled,
        bool expiryNotifyEnabled,
        bool failureNotifyEnabled,
        string? newSendKey,
        IReadOnlyDictionary<string, bool> tasks
    )
    {
        var key = string.IsNullOrWhiteSpace(newSendKey)
            ? ServerChanCookieExpiryNotifier.GetSendKey(configuration)
            : newSendKey.Trim();
        if (
            (expiryNotifyEnabled || failureNotifyEnabled || !string.IsNullOrWhiteSpace(newSendKey))
            && ServerChanCookieExpiryNotifier.CreateEndpoint(key) is null
        )
            throw new InvalidOperationException("请填写有效的 Server 酱 SendKey");
        if (
            tasks.Keys.Any(taskKey =>
                !TaskFailureNotificationCatalog.All.Any(task => task.TaskKey == taskKey)
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
        values["CookieCheck:AutoCheckEnabled"] = autoCheckEnabled.ToString();
        values["CookieCheck:NotifyEnabled"] = expiryNotifyEnabled.ToString();
        values["TaskFailureNotification:Enabled"] = failureNotifyEnabled.ToString();
        if (!string.IsNullOrWhiteSpace(newSendKey))
            values["CookieCheck:ServerChanSendKey"] = key!;
        provider.BatchSet(values);
        root!.Reload();
    }
}
