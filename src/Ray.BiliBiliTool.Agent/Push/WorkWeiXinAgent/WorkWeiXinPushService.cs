using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Push.WorkWeiXinAgent.Interfaces;

namespace Ray.BiliBiliTool.Agent.Push.WorkWeiXinAgent
{
    public class WorkWeiXinPushService : IPushService
    {
        private readonly IWorkWeiXinPushApi _workWeiXinPushApi;

        public WorkWeiXinPushService(IWorkWeiXinPushApi workWeiXinPushApi)
        {
            _workWeiXinPushApi = workWeiXinPushApi;
        }

        public string Name => "WorkWeiXin";

        public bool Send(string title, string content)
        {
            _workWeiXinPushApi.Send(new Dtos.WorkWeiXinPushRequest { Msgtype = "markdown", Markdown = new Dtos.Markdown { Content = content } });
            return true;
        }
    }
}
