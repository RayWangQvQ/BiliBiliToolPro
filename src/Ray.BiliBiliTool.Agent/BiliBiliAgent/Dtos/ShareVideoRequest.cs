using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
