using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace Ray.BiliBiliTool.Web.Services.Pages.Configs;

public sealed class CookieNotificationSettingsWorkflow(IConfiguration configuration)
    : ICookieNotificationSettingsWorkflow
{
    public CookieNotificationSettings Read()
    {
        var hasKey =
            ServerChanCookieExpiryNotifier.CreateEndpoint(
                ServerChanCookieExpiryNotifier.GetSendKey(configuration)
            )
            is not null;
        return new(
            configuration.GetValue("CookieCheck:AutoCheckEnabled", true),
            configuration.GetValue("CookieCheck:NotifyEnabled", hasKey),
            hasKey
        );
    }

    public void Save(bool autoCheckEnabled, bool notifyEnabled, string? newSendKey)
    {
        var key = string.IsNullOrWhiteSpace(newSendKey)
            ? ServerChanCookieExpiryNotifier.GetSendKey(configuration)
            : newSendKey.Trim();
        if (notifyEnabled && ServerChanCookieExpiryNotifier.CreateEndpoint(key) is null)
            throw new InvalidOperationException(
                "请填写有效的 Server 酱 SendKey（SCT 或 sctp 开头）"
            );

        var root = configuration as IConfigurationRoot;
        var provider =
            root?.Providers.OfType<SqliteConfigurationProvider>().LastOrDefault()
            ?? throw new InvalidOperationException("保存失败，请稍后重试");
        var values = new Dictionary<string, string>
        {
            ["CookieCheck:AutoCheckEnabled"] = autoCheckEnabled.ToString(),
            ["CookieCheck:NotifyEnabled"] = notifyEnabled.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(newSendKey))
            values["CookieCheck:ServerChanSendKey"] = key!;
        provider.BatchSet(values);
        root!.Reload();
    }
}
