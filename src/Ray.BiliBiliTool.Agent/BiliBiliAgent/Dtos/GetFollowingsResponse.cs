
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class GetFollowingsResponse
    {
        public List<UpInfo> List { get; set; }

        public int Total { get; set; }
    }
}
