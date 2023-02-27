using Newtonsoft.Json;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class EnterRoomRequest
    {
        public EnterRoomRequest(
            long roomId,
            long parentId,
            long areaID,
            int seqNumber, // 心跳包编号
            long timestamp,
            string userAgent,
            string csrf,
            long ruid,
            string device)
        {
            Id = JsonConvert.SerializeObject(new[] { parentId, areaID, seqNumber, roomId });
            Ts = timestamp;
            Ua = userAgent;
            Csrf = csrf;
            Ruid = ruid;

            Is_patch = 0;
            Heart_beat = "[]";
            Visit_id = "";
            Device = device;
        }
        public string Id { get; set; }

        public long Ruid { get; set; }

        public long Ts { get; set; }

        public int Is_patch { get; set; }

        public string Heart_beat { get; set; }

        public string Ua { get; set; }

        public string Csrf_token => Csrf;

        public string Csrf { get; set; }

        public string Visit_id { get; set; }

        public string Device { get; set; }
    }
}
