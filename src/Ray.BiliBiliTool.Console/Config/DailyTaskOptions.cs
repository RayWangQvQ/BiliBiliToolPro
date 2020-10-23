using System;
using System.Collections.Generic;
using System.Text;

namespace BiliBiliTool.Config
{
    /// <summary>
    /// 每日任务配置
    /// </summary>
    public class DailyTaskOptions
    {
        /// <summary>
        /// 每日设定的投币数 [0,5]
        /// </summary>
        public int NumberOfCoins { get; set; }

        /// <summary>
        /// 投币时是否点赞 [0,1]
        /// </summary>
        public int SelectLike { get; set; }

        /// <summary>
        /// 观看时是否分享 [0,1]
        /// </summary>
        public int WatchAndShare { get; set; }

        /// <summary>
        /// 年度大会员自动充电[false,true]
        /// </summary>
        public bool MonthEndAutoCharge { get; set; }

        /// <summary>
        /// 执行客户端操作时的平台 [ios,android]
        /// </summary>
        public string DevicePlatform { get; set; }
    }
}
