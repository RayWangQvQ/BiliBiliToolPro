using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.Push.WorkWeiXinAgent.Dtos
{
    public class WorkWeiXinPushRequest
    {
        public string Msgtype { get; set; }

        public Markdown Markdown { get; set; }
    }

    public class Markdown
    {
        public string Content { get; set; }
    }
}
