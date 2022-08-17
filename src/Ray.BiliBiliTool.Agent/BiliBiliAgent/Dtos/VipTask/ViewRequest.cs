using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask
{
    public class ViewRequest
    {
        public ViewRequest(string position)
        {
            this.position=position;
        }

        public string position { get; set; }

        public string c_locale { get; set; } = "zh_CN";

        public string channel { get; set; } = "html5_search_baidu";

        public int disable_rcmd { get; set; } = 0;

        public string mobi_app { get; set; } = "android";

        public string platform { get; set; } = "android";

        public string s_locale { get; set; } = "zh_CN";

        public string statistics { get; set; } = "{\"appId\":1,\"platform\":3,\"version\":\"6.85.0\",\"abtest\":\"\"}";
    }
}
