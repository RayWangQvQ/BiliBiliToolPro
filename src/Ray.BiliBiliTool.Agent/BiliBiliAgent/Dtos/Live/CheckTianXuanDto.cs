using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class CheckTianXuanDto
    {
        public int id { get; set; }

        public int room_id { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        /// <sample>1：可参与抽奖</sample>
        /// <sample>2：已出结果</sample>
        public int status { get; set; }

        /// <summary>
        /// 奖励名称
        /// </summary>
        public string award_name { get; set; }

        /// <summary>
        /// 奖励数量
        /// </summary>
        public int award_num { get; set; }

        /// <summary>
        /// 弹幕内容
        /// </summary>
        public string danmu { get; set; }

        public int join_type { get; set; }

        /// <summary>
        /// 要求条件类型
        /// </summary>
        /// <sample>1：关注主播</sample>
        /// <sample>2：粉丝勋章级数要求</sample>
        public int require_type { get; set; }

        /// <summary>
        /// 要求值
        /// </summary>
        public int require_value { get; set; }

        /// <summary>
        /// 要求名称
        /// </summary>
        public string require_text { get; set; }

        #region 礼物
        public int gift_id { get; set; }

        public string gift_name { get; set; }

        public int gift_num { get; set; }

        public int gift_price { get; set; }

        public int cur_gift_num { get; set; }
        #endregion

        public int send_gift_ensure { get; set; }
    }
}
