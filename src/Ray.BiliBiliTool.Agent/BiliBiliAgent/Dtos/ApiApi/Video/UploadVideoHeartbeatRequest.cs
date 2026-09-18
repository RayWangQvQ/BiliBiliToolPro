namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;

public class UploadVideoHeartbeatRequest
{
    public long aid { get; set; }

    /// <summary>
    /// 视频CID，用于识别分P
    /// </summary>
    public long? cid { get; set; }

    public required string bvid { get; set; }

    public long? epid { get; set; }

    public long? sid { get; set; }

    /// <summary>
    /// 当前用户UID
    /// </summary>
    public long mid { get; set; }

    public required string csrf { get; set; }

    /// <summary>
    /// 视频播放进度（即视频进度条的当前秒数），单位为秒，默认为0
    /// </summary>
    public int played_time { get; set; }

    public int real_played_time { get; set; }

    /// <summary>
    /// 总计播放时间，单位为秒
    /// </summary>
    public int realtime { get; set; }

    /// <summary>
    /// 开始播放时刻，时间戳
    /// </summary>
    public long start_ts { get; set; } = DateTime.Now.ToTimeStamp();

    /// <summary>
    /// 视频类型
    /// <sample>3：投稿视频</sample>
    /// <sample>4：剧集</sample>
    /// <sample>10：课程</sample>
    /// </summary>
    public int type { get; set; } = 3;

    /// <summary>
    /// 剧集副类型
    /// </summary>
    public int? sub_type { get; set; }

    /// <summary>
    /// 2
    /// </summary>
    public int dt { get; set; } = 2;

    /// <summary>
    /// 播放动作
    /// <sample>0：播放中</sample>
    /// <sample>1：开始播放</sample>
    /// <sample>2：暂停</sample>
    /// <sample>3：继续播放</sample>
    /// </summary>
    public int play_type { get; set; } = 3;
}
