using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class LikeLiveRoomRequest
    {
        public LikeLiveRoomRequest(int roomid, string csrf)
        {
            Roomid = roomid;
            Csrf= csrf;
        }

        public int Roomid { get; set; }

        public string Csrf { get; set; }

        public string Csrf_token => Csrf;
    }
}
