namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    /// <summary>
    /// 排行榜信息
    /// </summary>
    public class RankingInfo
    {
        public string Aid { get; set; }

        public string Bvid { get; set; }

        public string Typename { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public long Play { get; set; }
        public long Review { get; set; }
        public long Video_review { get; set; }
        public long Favorites { get; set; }

        public long Mid { get; set; }
        public string Author { get; set; }

        public string Description { get; set; }
        public string Create { get; set; }
        public string Pic { get; set; }
        public long Coins { get; set; }
        public string Duration { get; set; }
        public bool Badgepay { get; set; }
        public long Pts { get; set; }
        public string Redirect_url { get; set; }
        public Rights Rights { get; set; }
    }

    public class Rights
    {

    }
}
