namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;

public class Sign2Request
{
    /// <summary>
    /// 当前时间（毫秒）
    /// </summary>
    /// <sample>1748445354567</sample>
    public long t { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public string device { get; set; } = "phone";

    /// <summary>
    /// 当前时间（秒）
    /// </summary>
    /// <sample>1748445354</sample>
    public long ts => t / 1000;
}
