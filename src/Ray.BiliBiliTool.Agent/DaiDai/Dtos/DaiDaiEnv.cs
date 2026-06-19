using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.DaiDai.Dtos;

/// <summary>
/// 呆呆面板环境变量实体（对应 /api/envs 返回与提交的 data 项）
/// </summary>
public class DaiDaiEnv
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
