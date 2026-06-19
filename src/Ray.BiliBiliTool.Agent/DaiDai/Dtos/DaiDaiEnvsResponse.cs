using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.DaiDai.Dtos;

/// <summary>
/// GET /api/envs 的分页响应：{ "data": [ ... ], "total": n, "page": 1, "page_size": 20 }
/// </summary>
public class DaiDaiEnvsResponse
{
    [JsonPropertyName("data")]
    public List<DaiDaiEnv> Data { get; set; } = [];

    [JsonPropertyName("total")]
    public long Total { get; set; }
}
