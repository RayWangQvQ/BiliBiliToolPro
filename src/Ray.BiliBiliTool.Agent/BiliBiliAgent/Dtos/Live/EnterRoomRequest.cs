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
            int roomId,
            int parentId,
            int areaID,
            int seqNumber, // 心跳包编号
            long timestamp,
            string userAgent,
            string csrf,
            int ruid)
        {
            this.Id = JsonConvert.SerializeObject(new[] { parentId, areaID, seqNumber, roomId });
            this.Ts = timestamp;
            this.Ua = userAgent;
            this.Csrf = csrf;
            this.Ruid = ruid;

            this.Is_patch = 0;
            this.Heart_beat = "[]";
            this.Visit_id = "";
        }
        public string Id { get; set; }

        public int Ruid { get; set; }

        public long Ts { get; set; }

        public int Is_patch { get; set; }

        public string Heart_beat { get; set; }

        public string Ua { get; set; }

        public string Csrf_token => Csrf;

        public string Csrf { get; set; }

        public string Visit_id { get; set; }
    }
}
