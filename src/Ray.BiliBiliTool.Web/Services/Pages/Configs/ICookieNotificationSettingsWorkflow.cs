namespace Ray.BiliBiliTool.Web.Services.Pages.Configs;

public sealed record CookieNotificationSettings(
    bool AutoCheckEnabled,
    bool NotifyEnabled,
    bool HasSendKey
);

public interface ICookieNotificationSettingsWorkflow
{
    CookieNotificationSettings Read();
    void Save(bool autoCheckEnabled, bool notifyEnabled, string? newSendKey);
}
