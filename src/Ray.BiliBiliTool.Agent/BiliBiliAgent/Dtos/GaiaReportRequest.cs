using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

/// <summary>
/// Request body of the ExClimbWuzhi device fingerprint report
/// </summary>
public class GaiaReportRequest
{
    /// <summary>
    /// Device fingerprint payload (a JSON string whose fields are Bilibili's obfuscated keys)
    /// </summary>
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}
