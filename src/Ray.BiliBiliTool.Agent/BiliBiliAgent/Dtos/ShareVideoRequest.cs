using System;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class ShareVideoRequest
    {
        public ShareVideoRequest(long aid, string csrf)
        {
            Aid = aid;
            Csrf = csrf;
        }

        public long Aid { get; set; }

        public string Csrf { get; set; }

        public string Eab_x { get; set; } = "1";

        public string Ramval { get; set; } = $"{new Random().Next(3, 20)}";

        public string Source { get; set; } = "web_normal";

        public string Ga { get; set; } = "1";
    }
}
