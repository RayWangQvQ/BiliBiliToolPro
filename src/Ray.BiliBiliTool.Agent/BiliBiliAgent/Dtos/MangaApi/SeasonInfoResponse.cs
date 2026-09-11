using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.MangaApi;

/// <summary>
/// 赛季信息请求体（body 固定 {"type":1}）
/// </summary>
public class SeasonRequest
{
    [JsonPropertyName("type")]
    public int Type { get; set; } = 1;
}

/// <summary>
/// 赛季活动信息响应（user.v1.SeasonV2/GetSeasonInfo）。
/// data.day_task.book_task 即"今日推荐（0点更新）"的每日阅读书单。
/// </summary>
public class SeasonInfoResponse
{
    [JsonPropertyName("current_time")]
    public string? CurrentTime { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("season_id")]
    public long SeasonId { get; set; }

    [JsonPropertyName("day_task")]
    public DayTaskInfo? DayTask { get; set; }

    [JsonPropertyName("per_task")]
    public PerTaskInfo? PerTask { get; set; }
}

/// <summary>
/// 每日任务（书单 + 奖励档位 + 当前进度）
/// </summary>
public class DayTaskInfo
{
    /// <summary>
    /// 每日阅读书单（B 站每天 0 点更新，须读这些指定的书才能获得阅读经验）
    /// </summary>
    [JsonPropertyName("book_task")]
    public List<BookTaskItem>? BookTask { get; set; }

    /// <summary>
    /// 已读完本数（每本读满 read_min 分钟后 +1）
    /// </summary>
    [JsonPropertyName("reward_progress")]
    public int RewardProgress { get; set; }

    /// <summary>
    /// 累计读满 N 本对应的经验奖励：1本+5 / 2本+10 / 3本+20 / 4本+20 / 5本+30
    /// </summary>
    [JsonPropertyName("extra_rewards")]
    public List<ExtraRewardItem>? ExtraRewards { get; set; }
}

/// <summary>
/// 每日阅读书单中的一本
/// </summary>
public class BookTaskItem
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// 漫画 comic_id
    /// </summary>
    [JsonPropertyName("id")]
    public long ComicId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 需要读满的分钟数（默认 5）
    /// </summary>
    [JsonPropertyName("read_min")]
    public int ReadMin { get; set; }

    [JsonPropertyName("point")]
    public int Point { get; set; }

    /// <summary>
    /// 当前已读分钟数
    /// </summary>
    [JsonPropertyName("user_read_min")]
    public int UserReadMin { get; set; }

    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }
}

/// <summary>
/// 累计读满 N 本的额外奖励档位
/// </summary>
public class ExtraRewardItem
{
    [JsonPropertyName("book_num")]
    public int BookNum { get; set; }

    [JsonPropertyName("point")]
    public int Point { get; set; }
}

/// <summary>
/// 每日任务状态（阅读/分享各 5 分）
/// </summary>
public class PerTaskInfo
{
    [JsonPropertyName("read_point")]
    public int ReadPoint { get; set; }

    [JsonPropertyName("read_status")]
    public int ReadStatus { get; set; }

    [JsonPropertyName("push_point")]
    public int PushPoint { get; set; }

    [JsonPropertyName("push_status")]
    public int PushStatus { get; set; }
}
