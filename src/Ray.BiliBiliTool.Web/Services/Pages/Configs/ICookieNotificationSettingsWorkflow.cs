namespace Ray.BiliBiliTool.Web.Services.Pages.Configs;

public sealed record CookieNotificationSettings(bool AutoCheckEnabled, bool NotifyEnabled);

public interface ICookieNotificationSettingsWorkflow
{
    CookieNotificationSettings Read();
    void Save(bool autoCheckEnabled, bool notifyEnabled);
}
