using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation
{
    public class ModifyRelationRequest
    {
        public ModifyRelationRequest(long fid, string csrf)
        {
            Fid = fid;
            Csrf = csrf;
        }

        public long Fid { get; set; }

        public string Csrf { get; set; }

        /// <summary>
        /// 动作
        /// </summary>
        /// <sample>2:取关</sample>
        public int Act { get; set; } = 2;

        public int Re_src { get; set; } = 11;

        public string Jsonp { get; set; } = "jsonp";
    }
}
