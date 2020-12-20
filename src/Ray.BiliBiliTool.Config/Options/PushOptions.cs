using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Config.Options
{
    public class PushOptions
    {
        public string Strategy { get; set; }//todo：需要兼容之前已经配置过server酱的人

        public ServerChan ServerChan { get; set; }

        public WorkWeiXin WorkWeiXin { get; set; }
    }

    public class ServerChan
    {
        public string PushScKey { get; set; }
    }

    public class WorkWeiXin
    {
        public string Key { get; set; }
    }

    public class Other
    {

    }
}
