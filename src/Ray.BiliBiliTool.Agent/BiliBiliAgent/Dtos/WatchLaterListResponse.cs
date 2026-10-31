using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

/// <summary>
/// 稍后再看列表响应
/// </summary>
public class WatchLaterListResponse
{
    /// <summary>
    /// 视频列表
    /// </summary>
    public List<WatchLaterItem>? List { get; set; }

    /// <summary>
    /// 总数
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// 稍后再看视频项
/// </summary>
public class WatchLaterItem
{
    /// <summary>
    /// AV 号
    /// </summary>
    public long Aid { get; set; }

    /// <summary>
    /// BV 号
    /// </summary>
    public string? Bvid { get; set; }

    /// <summary>
    /// 视频标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// UP 主
    /// </summary>
    public WatchLaterOwner? Owner { get; set; }

    /// <summary>
    /// 视频时长（秒）
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// UP 主 ID
    /// </summary>
    public long Mid { get; set; }

    /// <summary>
    /// 添加时间（时间戳）
    /// </summary>
    [JsonPropertyName("add_at")]
    public long AddTime { get; set; }

    /// <summary>
    /// 视频封面
    /// </summary>
    public string? Pic { get; set; }

    /// <summary>
    /// 视频状态码（1=已看完，2=被删除等）
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// 已观看进度（秒）
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 是否已看完（严格标准：progress >= duration）
    /// </summary>
    public bool IsFullyViewed => Duration > 0 && Progress >= Duration;

    /// <summary>
    /// 根据指定阈值判断是否已看完（threshold 取值 0.0~1.0）
    /// </summary>
    public bool IsViewedByThreshold(double threshold)
    {
        if (Duration <= 0)
            return false;
        if (threshold <= 0)
            return Progress > 0;
        if (threshold >= 1.0)
            return IsFullyViewed;
        return (double)Progress / Duration >= threshold;
    }

    /// <summary>
    /// 是否有效（未删除）
    /// </summary>
    public bool IsValid => State == 0;
}

/// <summary>
/// 视频作者
/// </summary>
public class WatchLaterOwner
{
    public long Mid { get; set; }
    public string? Name { get; set; }
    public string? Face { get; set; }
}
