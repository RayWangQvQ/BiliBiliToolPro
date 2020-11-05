using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Agent.ServerChanAgent.Dtos
{
    public class PushResponse
    {
        public int Errno { get; set; }

        public string Errmsg { get; set; }

        public string Dataset { get; set; }
    }
}
