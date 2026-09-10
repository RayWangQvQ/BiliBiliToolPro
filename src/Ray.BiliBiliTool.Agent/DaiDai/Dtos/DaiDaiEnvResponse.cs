using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.DaiDai.Dtos;

/// <summary>
/// POST/PUT /api/envs 的单条响应：{ "message": "...", "data": { ... } }
/// </summary>
public class DaiDaiEnvResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public DaiDaiEnv Data { get; set; }
}
