using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Config
{
    public static class Constants
    {
        /// <summary>
        /// 每天的最大投币数，优先级最高，默认每天最多投5个币（包含已投过的数量）
        /// </summary>
        public static int MaxNumberOfDonateCoins = 5;
    }
}
