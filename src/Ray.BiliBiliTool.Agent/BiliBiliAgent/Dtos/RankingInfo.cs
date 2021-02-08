using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class Ranking
    {
        public List<RankingInfo> List { get; set; }
    }

    /// <summary>
    /// 排行榜信息
    /// </summary>
    public class RankingInfo
    {
        public long Aid { get; set; }

        public string Bvid { get; set; }

        public long Cid { get; set; }

        public string Title { get; set; }

        /// <summary>
        /// 是否转载
        /// <sample>1：原创</sample>
        /// <sample>2：转载</sample>
        /// </summary>
        public int Copyright { get; set; }

        public int Duration { get; set; }
    }
}
