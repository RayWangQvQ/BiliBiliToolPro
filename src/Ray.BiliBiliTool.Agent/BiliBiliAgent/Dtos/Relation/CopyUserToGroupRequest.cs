using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation
{
    public class CopyUserToGroupRequest
    {
        public CopyUserToGroupRequest(List<long> fids, string tagid, string csrf)
        {
            Fids = string.Join(",", fids);
            Tagids = tagid;
            Csrf = csrf;
        }

        public string Fids { get; set; }

        public string Tagids { get; set; }

        public string Csrf { get; set; }

        public string Jsonp { get; set; } = "jsonp";

    }
}
