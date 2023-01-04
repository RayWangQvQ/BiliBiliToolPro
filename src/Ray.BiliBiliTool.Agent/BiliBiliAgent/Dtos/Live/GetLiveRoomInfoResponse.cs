using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class GetLiveRoomInfoResponse
    {
        public int Room_id { get; set; }

        public int Area_id { get; set; }

        public int Parent_area_id { get; set; }

        public int Uid { get; set; }
    }
}
