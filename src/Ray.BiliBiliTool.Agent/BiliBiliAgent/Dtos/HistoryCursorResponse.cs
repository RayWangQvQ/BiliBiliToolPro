using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

/// <summary>
/// 历史记录分页响应
/// </summary>
public class HistoryCursorResponse
{
    /// <summary>
    /// 翻页游标
    /// </summary>
    public HistoryCursor Cursor { get; set; } = new();

    /// <summary>
    /// 历史记录列表
    /// </summary>
    public List<HistoryItem> List { get; set; } = new();
}

public class HistoryCursor
{
    /// <summary>
    /// 最大 view_at（用于翻到下一页）
    /// </summary>
    public long Max { get; set; }

    /// <summary>
    /// 当前 view_at
    /// </summary>
    [JsonPropertyName("view_at")]
    public long ViewAt { get; set; }

    /// <summary>
    /// 业务类型，archive = 视频
    /// </summary>
    public string Business { get; set; } = "archive";

    /// <summary>
    /// 每页条数
    /// </summary>
    public int Ps { get; set; }
}

/// <summary>
/// 单条历史记录（与稍后再看对比用的最小字段集）
/// </summary>
public class HistoryItem
{
    /// <summary>
    /// 视频标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 真实已观看进度（秒）。看完的视频 = duration
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 视频总时长（秒）
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// 内部子对象，包含 bvid / oid 等
    /// </summary>
    public HistoryDetail? History { get; set; }
}

public class HistoryDetail
{
    /// <summary>
    /// 视频 BV 号
    /// </summary>
    public string Bvid { get; set; } = "";

    /// <summary>
    /// 视频 aid (kid 形式，与 toview.aid 不一致，仅供参考)
    /// </summary>
    public long Oid { get; set; }
}
