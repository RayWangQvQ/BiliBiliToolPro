using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class AddCoinRequest
    {
        public AddCoinRequest(long aid, string csrf)
        {
            Aid = aid;
            Csrf = csrf;
        }

        public long Aid { get; set; }

        public int Multiply { get; set; } = 1;

        public int Select_like { get; set; } = 1;

        public string Cross_domain { get; set; } = "true";

        public string Csrf { get; set; }

        public string Eab_x { get; set; } = "2";

        public string Ramval { get; set; } = "3";

        public string Source { get; set; } = "web_normal";

        public string Ga { get; set; } = "1";
    }
}
