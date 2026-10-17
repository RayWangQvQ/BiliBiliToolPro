using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

/// <summary>
/// Response of the finger/spi endpoint, carrying the fresh buvid3 (b_3) / buvid4 (b_4) pair.
/// </summary>
public sealed class DeviceFingerprintResponse
{
    [JsonPropertyName("b_3")]
    public string? B_3 { get; set; }

    [JsonPropertyName("b_4")]
    public string? B_4 { get; set; }
}
