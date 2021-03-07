using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class CheckTianXuanDto
    {
        public int Id { get; set; }

        public int Room_id { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        /// <sample>1：可参与抽奖</sample>
        /// <sample>2：已出结果</sample>
        public int Status { get; set; }

        /// <summary>
        /// 奖励名称
        /// </summary>
        public string Award_name { get; set; }

        /// <summary>
        /// 奖励数量
        /// </summary>
        public int Award_num { get; set; }

        /// <summary>
        /// 弹幕内容
        /// </summary>
        public string Danmu { get; set; }

        public int Join_type { get; set; }

        /// <summary>
        /// 要求条件类型
        /// </summary>
        /// <sample>1：关注主播</sample>
        /// <sample>2：粉丝勋章级数要求</sample>
        /// <sample>3：成为主播的提督舰长等</sample>
        public int Require_type { get; set; }

        /// <summary>
        /// 要求值
        /// </summary>
        public int Require_value { get; set; }

        /// <summary>
        /// 要求名称
        /// </summary>
        public string Require_text { get; set; }

        #region 礼物
        public int Gift_id { get; set; }

        public string Gift_name { get; set; }

        public int Gift_num { get; set; }

        public int Gift_price { get; set; }

        public int Cur_gift_num { get; set; }
        #endregion

        public int Send_gift_ensure { get; set; }
    }
}
