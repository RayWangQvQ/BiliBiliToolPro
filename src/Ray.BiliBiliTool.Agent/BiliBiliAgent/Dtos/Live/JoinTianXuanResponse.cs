using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class JoinTianXuanResponse
    {
        public int Discount_id { get; set; }

        public int Gold { get; set; }

        public int Silver { get; set; }

        public int Cur_gift_num { get; set; }

        public int Goods_id { get; set; }

        public string New_order_id { get; set; }
    }
}
