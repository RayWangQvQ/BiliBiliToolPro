using System.Collections.Generic;

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
        public int NumberOfCoins { get; set; } = 5;

        /// <summary>
        /// 投币时是否点赞[false,true]
        /// </summary>
        public bool SelectLike { get; set; } = false;

        /// <summary>
        /// 优先选择支持的up主Id集合，配置后会优先从指定的up主下挑选视频进行观看、分享和投币，不配置则从排行耪随机获取支持视频
        /// </summary>
        public string SupportUpIds { get; set; } = "220893216";

        /// <summary>
        /// 每月几号自动充电[-1,31]，-1表示不指定，默认月底最后一天；0表示不充电
        /// </summary>
        public int DayOfAutoCharge { get; set; } = -1;

        /// <summary>
        /// 充电Up主Id
        /// </summary>
        public string AutoChargeUpId { get; set; } = "220893216";

        /// <summary>
        /// 充电后留言
        /// </summary>
        public string ChargeComment { get; set; } = "加油~";

        /// <summary>
        /// 每月几号自动领取会员权益的[-1,31]，-1表示不指定，默认每月1号；0表示不自动领取
        /// </summary>
        public int DayOfReceiveVipPrivilege { get; set; } = -1;

        /// <summary>
        /// 是否开启直播中心银瓜子兑换硬币
        /// </summary>
        public bool IsExchangeSilver2Coin { get; set; } = true;

        /// <summary>
        /// 执行客户端操作时的平台 [ios,android]
        /// </summary>
        public string DevicePlatform { get; set; } = "ios";


        public List<long> SupportUpIdList
        {
            get
            {
                var re = new List<long>();
                if (string.IsNullOrWhiteSpace(SupportUpIds) | SupportUpIds == "-1") return re;

                var array = SupportUpIds.Split(',');
                foreach (var item in array)
                {
                    if (int.TryParse(item.Trim(), out int upId))
                        re.Add(upId);
                }
                return re;
            }
        }

    }
}
