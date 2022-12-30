using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class HeartBeatResponse
    {
        public int Heartbeat_interval { get; set; }

        public string Secret_key { get; set; }

        public List<int> Secret_rule { get; set;}

        public long Timestamp { get; set; }  
    }
}
