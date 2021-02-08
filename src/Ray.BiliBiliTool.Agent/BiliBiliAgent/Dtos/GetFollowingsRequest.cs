using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class GetFollowingsRequest
    {
        public GetFollowingsRequest(long userId)
        {
            Vmid = userId;
        }

        public long Vmid { get; set; }

        public int Pn { get; set; } = 1;

        public int Ps { get; set; } = 20;

        public string Order { get; set; } = "desc";

        public string Order_type { get; set; } = "attention";

        public string Jsonp { get; set; } = "jsonp";

        //public string Callback { get; set; }
    }
}
