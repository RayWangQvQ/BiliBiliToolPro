using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Config
{
    public static class CommandLineMapper
    {
        public static readonly Dictionary<string, string> Mapper = new Dictionary<string, string>
        {
            {"-userId","BiliBiliCookies:UserId" },
            {"-sessData","BiliBiliCookies:SessData" },
            {"-biliJct","BiliBiliCookies:BiliJct" },
        };
    }
}
