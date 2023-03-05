using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class SendLiveDanmukuRequest
    {
        public SendLiveDanmukuRequest(string csrf, long room_id, string message)
        {
            this.Csrf = csrf;
            this.Msg= message;
            this.Roomid= room_id;
            this.Bubble = "0";
            this.Mode = "1";
            this.Fontsize = "25";
            this.Rnd = "1672305761";
            this.Color = "16777215";
        }
        public string Bubble { get; set; }

        public string Msg { get; set; }

        public string Color { get; set; }

        public string Mode { get; set; }

        public string Fontsize { get; set; }

        public string Rnd { get; set; }

        public long Roomid { get; set; }

        public string Csrf { get; set; }

        public string Csrf_token => Csrf;
    }
}
