using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.DaiDai.Dtos;

/// <summary>
/// POST /api/open-api/token 的请求体
/// </summary>
public class DaiDaiTokenRequest
{
    [JsonPropertyName("app_key")]
    public string AppKey { get; set; }

    [JsonPropertyName("app_secret")]
    public string AppSecret { get; set; }
}
