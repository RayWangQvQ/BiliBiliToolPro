using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class LikeLiveRoomRequest
    {
        public LikeLiveRoomRequest(long roomid, string csrf, int clickTime, long anchorId, string uid)
        {
            Roomid = roomid;
            Csrf = csrf;
            Click_Time = clickTime;
            Anchor_Id = anchorId;
            Uid = uid;
        }

        public long Roomid { get; set; }

        public string Csrf { get; set; }

        public string Csrf_Token => Csrf;

        public int Click_Time { get; set; }

        public long Anchor_Id { get; set; }

        public string Uid { get; set; }


        public string RawTextBuild()
        {
            return
                $"click_time={Click_Time.ToString()}&room_id={Roomid.ToString()}&uid={Uid}&anchor_id={Anchor_Id}&csrf_token={Csrf_Token}&csrf={Csrf}";
        }
    }
}