using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class GetLiveRoomInfoResponse
    {
        public long Room_id { get; set; }

        public long Area_id { get; set; }

        public long Parent_area_id { get; set; }

        public long Uid { get; set; }
    }
}
