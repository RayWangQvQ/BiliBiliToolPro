using Ray.BiliBiliTool.Config.SQLite;

namespace Ray.BiliBiliTool.Web.Services.Pages.Configs;

public sealed class CookieNotificationSettingsWorkflow(IConfiguration configuration)
    : ICookieNotificationSettingsWorkflow
{
    public CookieNotificationSettings Read() =>
        new(
            configuration.GetValue("CookieCheck:AutoCheckEnabled", true),
            configuration.GetValue("CookieCheck:NotifyEnabled", true)
        );

    public void Save(bool autoCheckEnabled, bool notifyEnabled)
    {
        var root = configuration as IConfigurationRoot;
        var provider =
            root?.Providers.OfType<SqliteConfigurationProvider>().LastOrDefault()
            ?? throw new InvalidOperationException("保存失败，请稍后重试");
        provider.BatchSet(
            new Dictionary<string, string>
            {
                ["CookieCheck:AutoCheckEnabled"] = autoCheckEnabled.ToString(),
                ["CookieCheck:NotifyEnabled"] = notifyEnabled.ToString(),
            }
        );
        root!.Reload();
    }
}
