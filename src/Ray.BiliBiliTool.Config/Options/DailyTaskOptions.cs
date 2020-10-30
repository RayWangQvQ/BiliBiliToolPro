namespace Ray.BiliBiliTool.Config.Options
{
    /// <summary>
    /// 程序自定义个性化配置
    /// </summary>
    public class DailyTaskOptions
    {
        /// <summary>
        /// 每日设定的投币数 [0,5]
        /// </summary>
        public int NumberOfCoins { get; set; }

        /// <summary>
        /// 投币时是否点赞[false,true]
        /// </summary>
        public bool SelectLike { get; set; }

        /// <summary>
        /// 年度大会员自动充电[false,true]
        /// </summary>
        public bool MonthEndAutoCharge { get; set; }//todo:是否可以改为指定每月的几号进行充电

        /// <summary>
        /// 执行客户端操作时的平台 [ios,android]
        /// </summary>
        public string DevicePlatform { get; set; }
    }
}
