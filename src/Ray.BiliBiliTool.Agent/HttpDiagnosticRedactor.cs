using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Ray.BiliBiliTool.Agent;

public static class HttpDiagnosticRedactor
{
    public const int MaximumLength = 2000;
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "csrf",
        "csrftoken",
        "bilijct",
        "sessdata",
        "accesskey",
        "accesstoken",
        "refreshtoken",
        "appsecret",
        "apikey",
        "password",
        "pwd",
        "token",
        "clientsecret",
        "sendkey",
        "qrcodekey",
        "buvid",
        "buvid3",
        "buvid4",
        "livebuvid",
        "biliticket",
        "ticket",
        "authorization",
        "proxyauthorization",
        "cookie",
        "setcookie",
        "secretkey",
        "gotifykey",
    };
    private static readonly Regex Parameters = new(
        @"(?<prefix>^|[?&;\s])(?<key>[a-zA-Z0-9_%.-]+)=(?<value>[^&;\r\n]*)",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250)
    );

    public static string RedactUri(Uri? uri)
    {
        if (uri is null)
            return "";
        var value = uri.OriginalString;
        if (uri.IsAbsoluteUri && !string.IsNullOrEmpty(uri.UserInfo))
        {
            var builder = new UriBuilder(uri) { UserName = "***", Password = "" };
            value = builder.Uri.OriginalString;
        }
        return Limit(RedactParameters(value));
    }

    public static string RedactHeader(string name, string value) =>
        IsSensitive(name) ? "***" : Limit(RedactParameters(value));

    public static string RedactBody(string body, string? mediaType = null)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;
        if (
            mediaType?.Equals(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase
            ) == true
        )
            return Limit(RedactParameters(body));
        var text = body.TrimStart().TrimStart('\uFEFF').TrimStart();
        if (
            text.StartsWith('{')
            || text.StartsWith('[')
            || mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true
        )
        {
            try
            {
                var node = JsonNode.Parse(text);
                if (node is not JsonObject && node is not JsonArray)
                    return $"[JSON 标量正文已隐藏，长度 {body.Length}]";
                RedactNode(node);
                return Limit(node.ToJsonString());
            }
            catch (JsonException)
            {
                return $"[无法解析的 JSON 正文已隐藏，长度 {body.Length}]";
            }
        }
        return $"[非 JSON 正文已隐藏，长度 {body.Length}]";
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject document)
        {
            foreach (var property in document.ToArray())
            {
                if (IsSensitive(property.Key))
                    document[property.Key] = "***";
                else if (
                    property.Value is JsonValue value
                    && value.TryGetValue<string>(out var text)
                )
                    document[property.Key] = RedactParameters(text);
                else
                    RedactNode(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue value && value.TryGetValue<string>(out var text))
                    array[index] = RedactParameters(text);
                else
                    RedactNode(array[index]);
            }
        }
    }

    private static bool IsSensitive(string name)
    {
        var normalized = new string(
            WebUtility.UrlDecode(name).Where(char.IsAsciiLetterOrDigit).ToArray()
        );
        if (normalized.StartsWith('x') || normalized.StartsWith('X'))
            normalized = normalized[1..];
        return SensitiveNames.Contains(normalized)
            || normalized.EndsWith("token", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("secret", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("apikey", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("auroraeid", StringComparison.OrdinalIgnoreCase);
    }

    private static string RedactParameters(string value)
    {
        try
        {
            return Parameters.Replace(
                value,
                match =>
                    IsSensitive(match.Groups["key"].Value)
                        ? match.Groups["prefix"].Value + match.Groups["key"].Value + "=***"
                        : match.Value
            );
        }
        catch (RegexMatchTimeoutException)
        {
            return "[诊断内容已隐藏]";
        }
    }

    private static string Limit(string value) =>
        value.Length <= MaximumLength ? value : value[..MaximumLength] + "...(已截断)";
}
