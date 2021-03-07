using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class JoinTianXuanRequest
    {
        /// <summary>
        /// Id（从Check接口获取）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 礼物Id（从Check接口获取）
        /// </summary>
        public int gift_id { get; set; }

        /// <summary>
        /// 礼物数量（从Check接口获取）
        /// </summary>
        public int gift_num { get; set; }

        /// <summary>
        /// bili_jct（取自Cookie）
        /// </summary>
        public string csrf { get; set; }

        public string csrf_token => csrf;

        /// <summary>
        /// 
        /// </summary>
        /// <sample>8u0w3cesz1o0</sample>
        public string visit_id { get; set; }

        public string platform { get; set; } = "pc";
    }
}
