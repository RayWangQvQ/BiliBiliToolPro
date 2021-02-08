using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class UploadVideoHeartbeatRequest
    {
        public long Aid { get; set; }

        /// <summary>
        /// 视频CID，用于识别分P
        /// </summary>
        public long? Cid { get; set; }

        public string Bvid { get; set; }

        /// <summary>
        /// 当前用户UID
        /// </summary>
        public long Mid { get; set; }

        public string Csrf { get; set; }

        /// <summary>
        /// 视频播放进度（即视频进度条的当前秒数），单位为秒，默认为0
        /// </summary>
        public int Played_time { get; set; }

        public int Real_played_time { get; set; }

        /// <summary>
        /// 总计播放时间，单位为秒
        /// </summary>
        public int Realtime { get; set; }

        /// <summary>
        /// 开始播放时刻，时间戳
        /// </summary>
        public long Start_ts { get; set; } = DateTime.Now.ToTimeStamp();

        /// <summary>
        /// 视频类型
        /// <sample>3：投稿视频</sample>
        /// <sample>4：剧集</sample>
        /// <sample>10：课程</sample>
        /// </summary>
        public int Type { get; set; } = 3;

        /// <summary>
        /// 2
        /// </summary>
        public int Dt { get; set; } = 2;

        /// <summary>
        /// 播放动作
        /// <sample>0：播放中</sample>
        /// <sample>1：开始播放</sample>
        /// <sample>2：暂停</sample>
        /// <sample>3：继续播放</sample>
        /// </summary>
        public int Play_type { get; set; } = 3;
    }
}
