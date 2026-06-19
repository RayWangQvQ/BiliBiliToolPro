using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.DaiDai.Dtos;

/// <summary>
/// POST /api/open-api/token 的响应：{ "data": { access_token, token_type, expires_in } }
/// </summary>
public class DaiDaiTokenResponse
{
    [JsonPropertyName("data")]
    public DaiDaiTokenData Data { get; set; }
}

public class DaiDaiTokenData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; }
}
