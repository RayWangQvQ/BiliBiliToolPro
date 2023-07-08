using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace Ray.BiliTool.Blazor.Web.Pages.Configs.Daily
{
    public class DailyTaskConfigModel
    {
        /// <summary>
        /// 是否观看视频
        /// </summary>
        [DisplayName("开启观看视频")]
        public bool IsWatchVideo { get; set; }

        /// <summary>
        /// 是否分享视频
        /// </summary>
        [DisplayName("开启分享视频")]
        public bool IsShareVideo { get; set; }

        /// <summary>
        /// 每日设定的投币数 [0,5]
        /// </summary>
        [DisplayName("每日投币数量")]
        public int NumberOfCoins { get; set; } = 5;

        /// <summary>
        /// 要保留的硬币数量 [0,int_max]
        /// </summary>
        [DisplayName("保留硬币数量")]
        public int NumberOfProtectedCoins { get; set; } = 0;

        /// <summary>
        /// 达到六级后是否开始白嫖
        /// </summary>
        [DisplayName("六级后开启白嫖模式")]
        public bool SaveCoinsWhenLv6 { get; set; } = false;

        /// <summary>
        /// 投币时是否点赞[false,true]
        /// </summary>
        [DisplayName("投币的同时点赞")]
        public bool SelectLike { get; set; } = false;

        /// <summary>
        /// 优先选择支持的up主Id集合，配置后会优先从指定的up主下挑选视频进行观看、分享和投币，不配置则从排行耪随机获取支持视频
        /// </summary>
        [DisplayName("优先支持的UP")]
        public string SupportUpIds { get; set; }

        /// <summary>
        /// 每月几号自动充电[-1,31]，-1表示不指定，默认月底最后一天；0表示不充电
        /// </summary>
        [DisplayName("充电日期")]
        public int DayOfAutoCharge { get; set; } = -1;

        /// <summary>
        /// 充电Up主Id
        /// </summary>
        [DisplayName("充电UP")]
        public string AutoChargeUpId { get; set; }

        /// <summary>
        /// 充电后留言
        /// </summary>
        [DisplayName("充电后留言")]
        public string ChargeComment { get; set; }

        /// <summary>
        /// 每月几号自动领取会员权益的[-1,31]，-1表示不指定，默认每月1号；0表示不自动领取
        /// </summary>
        [DisplayName("领取会员权益日期")]
        public int DayOfReceiveVipPrivilege { get; set; } = -1;

        /// <summary>
        /// 每月几号执行银瓜子兑换硬币[-1,31]，-1表示不指定，默认每月1号；-2表示每天；0表示不进行兑换
        /// </summary>
        [DisplayName("银瓜子兑换硬币日期")]
        public int DayOfExchangeSilver2Coin { get; set; } = -1;

        /// <summary>
        /// 自定义漫画阅读 comic_id
        /// </summary>
        [DisplayName("阅读漫画Id")]
        public long CustomComicId { get; set; } = 27355;

        /// <summary>
        /// 自定义漫画阅读 ep_id
        /// </summary>
        [DisplayName("阅读漫画EpId")]
        public long CustomEpId { get; set; } = 381662;
    }

}
