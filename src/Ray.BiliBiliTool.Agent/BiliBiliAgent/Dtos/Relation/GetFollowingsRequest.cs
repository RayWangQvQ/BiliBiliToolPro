using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class GetFollowingsRequest
    {
        public GetFollowingsRequest(
            long userId,
            FollowingsOrderType followingsOrder = FollowingsOrderType.AttentionDesc
            )
        {
            Vmid = userId;
            Order_type = followingsOrder.DefaultValue();
        }

        public long Vmid { get; set; }

        public string Order_type { get; set; }

        public int Pn { get; set; } = 1;

        public int Ps { get; set; } = 20;

        public string Order { get; set; } = "desc";

        public string Jsonp { get; set; } = "jsonp";

        //public string Callback { get; set; }
    }
}
