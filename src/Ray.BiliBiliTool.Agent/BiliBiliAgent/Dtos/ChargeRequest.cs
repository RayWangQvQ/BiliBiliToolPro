using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class ChargeRequest
    {
        public ChargeRequest(decimal bp_num, long upId, string csrf)
        {
            Bp_num = bp_num;
            Up_mid = upId;
            Oid = upId;
            Csrf = csrf;
        }

        /// <summary>
        /// B币个数
        /// </summary>
        public decimal Bp_num { get; set; }

        public string Is_bp_remains_prior { get; set; } = "true";

        /// <summary>
        /// 对方Id
        /// </summary>
        public long Up_mid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Otype { get; set; } = "up";

        /// <summary>
        /// 对方来源代码(空间充电：充电对象用户UID;视频充电：稿件avID)
        /// </summary>
        public long Oid { get; set; }

        /// <summary>
        /// 自己的bili_jct
        /// </summary>
        public string Csrf { get; set; }
    }
}
