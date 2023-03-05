using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class JoinTianXuanResponse
    {
        public long Discount_id { get; set; }

        public long Gold { get; set; }

        public long Silver { get; set; }

        public long Cur_gift_num { get; set; }

        public long Goods_id { get; set; }

        public string New_order_id { get; set; }
    }
}
