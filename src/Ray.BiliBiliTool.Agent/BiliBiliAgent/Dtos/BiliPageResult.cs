namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class BiliPageResult
    {
        /// <summary>
        /// 视频总数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Pn { get; set; }

        /// <summary>
        /// 每页条数
        /// </summary>
        public int Ps { get; set; }
    }
}
