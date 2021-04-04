using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation
{
    public class CopyUserToGroupRequest
    {
        public string Fids { get; set; }

        public string Tagids { get; set; }

        public string Jsonp { get; set; } = "jsonp";

        public string Csrf { get; set; }
    }
}
