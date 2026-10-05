using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Cookies;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public sealed class ServerChanCookieExpiryNotifier(
    IHttpClientFactory clientFactory,
    IConfiguration configuration
) : ICookieExpiryNotifier
{
    public const string ClientName = "CookieExpiryServerChan";

    public static string? GetSendKey(IConfiguration configuration)
    {
        var key = configuration["CookieCheck:ServerChanSendKey"];
        if (!string.IsNullOrWhiteSpace(key))
            return key.Trim();
        return configuration
            .GetSection("Serilog:WriteTo")
            .GetChildren()
            .FirstOrDefault(section =>
                string.Equals(
                    section["Name"],
                    "ServerChanBatched",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            ?["Args:turboScKey"]?.Trim();
    }

    public static Uri? CreateEndpoint(string? sendKey)
    {
        if (string.IsNullOrWhiteSpace(sendKey))
            return null;
        if (Regex.IsMatch(sendKey, "^SCT[A-Za-z0-9]+$"))
            return new Uri($"https://sctapi.ftqq.com/{sendKey}.send");
        var match = Regex.Match(sendKey, "^sctp([0-9]+)t[A-Za-z0-9]+$");
        return match.Success
            ? new Uri($"https://{match.Groups[1].Value}.push.ft07.com/send/{sendKey}.send")
            : null;
    }

    public async Task<bool> SendAsync(string maskedAccount, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("CookieCheck:NotifyEnabled", true))
            return false;
        var endpoint = CreateEndpoint(GetSendKey(configuration));
        if (endpoint is null)
            return false;

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["title"] = "BiliBili Cookie 已过期",
                ["desp"] =
                    $"账号 {maskedAccount} 的登录凭证已失效，本次活动已自动跳过。\n\n请打开 BiliBiliToolPro 面板，在「账号管理」中重新扫码登录。\n\n同一账号每天最多发送一次过期提醒。",
            }
        );
        using var response = await clientFactory
            .CreateClient(ClientName)
            .PostAsync(endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        return body.RootElement.TryGetProperty("code", out var code)
            && code.TryGetInt32(out var value)
            && value == 0;
    }
}
