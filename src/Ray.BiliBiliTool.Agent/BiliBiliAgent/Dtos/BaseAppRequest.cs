namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class BaseAppRequest
{
    // public string access_key { get; set; }
    // public string appkey { get; set; }

    public string build { get; } = "8451100";

    public int disable_rcmd { get; } = 0;

    public string mobi_app { get; } = "android";

    public string platform { get; } = "android";

    public string statistics { get; } =
        "{\"appId\":1,\"platform\":3,\"version\":\"8.45.1\",\"abtest\":\"\"}";

    /// <summary>
    /// 当前时间（毫秒）
    /// </summary>
    /// <sample>1748445354567</sample>
    public long t { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 当前时间（秒）
    /// </summary>
    /// <sample>1748445354</sample>
    public long ts => t / 1000;

    public string? sign { get; set; }
}
