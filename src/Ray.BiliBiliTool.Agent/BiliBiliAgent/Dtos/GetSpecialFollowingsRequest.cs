using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class GetSpecialFollowingsRequest
    {
        public GetSpecialFollowingsRequest(long userId)
        {
            Mid = userId;
        }

        public long Mid { get; set; }

        public int Tagid { get; set; } = -10;

        public int Pn { get; set; } = 1;

        public int Ps { get; set; } = 20;

        public string Jsonp { get; set; } = "jsonp";

        //public string Callback { get; set; }
    }
}
