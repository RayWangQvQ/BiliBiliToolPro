using System;
using System.Collections.Generic;

namespace Ray.BiliBiliTool.Config.Options
{
    /// <summary>
    /// 程序自定义个性化配置
    /// </summary>
    public class DailyTaskOptions
    {
        /// <summary>
        /// 是否观看视频
        /// </summary>
        public bool IsWatchVideo { get; set; }

        /// <summary>
        /// 是否分享视频
        /// </summary>
        public bool IsShareVideo { get; set; }

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
        public string AutoChargeUpId { get; set; } = "1585227649";

        private string _chargeComment;
        /// <summary>
        /// 充电后留言
        /// </summary>
        public string ChargeComment
        {
            get =>
                string.IsNullOrWhiteSpace(_chargeComment)
                    ? DefaultComments[new Random().Next(0, DefaultComments.Count)]
                    : _chargeComment;
            set => _chargeComment = value;
        }

        /// <summary>
        /// 每月几号自动领取会员权益的[-1,31]，-1表示不指定，默认每月1号；0表示不自动领取
        /// </summary>
        public int DayOfReceiveVipPrivilege { get; set; } = -1;

        /// <summary>
        /// 每月几号执行银瓜子兑换硬币[-1,31]，-1表示不指定，默认每月1号；-2表示每天；0表示不进行兑换
        /// </summary>
        public int DayOfExchangeSilver2Coin { get; set; } = -1;

        /// <summary>
        /// 执行客户端操作时的平台 [ios,android]
        /// </summary>
        public string DevicePlatform { get; set; } = "android";


        public List<long> SupportUpIdList
        {
            get
            {
                var re = new List<long>();
                if (string.IsNullOrWhiteSpace(SupportUpIds) | SupportUpIds == "-1") return re;

                var array = SupportUpIds.Split(',');
                foreach (var item in array)
                {
                    if (long.TryParse(item.Trim(), out long upId))
                        re.Add(upId);
                    else
                        re.Add(long.MinValue);
                }
                return re;
            }
        }

        private static List<string> DefaultComments = new List<string>
        {
            "棒",
            "棒唉",
            "棒耶",
            "加油~",
            "UP加油!",
            "支持~",
            "支持支持！",
            "催更啦",
            "顶顶",
            "留下脚印~",
            "干杯",
            "bilibili干杯",
            "o(*￣▽￣*)o",
            "(｡･∀･)ﾉﾞ嗨",
            "(●ˇ∀ˇ●)",
            "( •̀ ω •́ )y",
            "(ง •_•)ง",
            ">.<",
            "^_~",
        };
    }
}
