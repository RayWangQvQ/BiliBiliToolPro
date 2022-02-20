using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class LiveWalletStatusResponse
    {
        /// <summary>
        /// 硬币余额
        /// </summary>
        public decimal Coin { get; set; }

        /// <summary>
        /// 金瓜子余额
        /// </summary>
        public decimal Gold { get; set; }

        /// <summary>
        /// 银瓜子余额
        /// </summary>
        public decimal Silver { get; set; }

        /// <summary>
        /// 银瓜子兑换硬币剩余次数
        /// </summary>
        public int Silver_2_coin_left { get; set; }

        /// <summary>
        /// 硬币兑换银瓜子剩余次数
        /// </summary>
        public int Coin_2_silver_left { get; set; }
    }
}
