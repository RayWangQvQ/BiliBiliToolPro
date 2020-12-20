using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Push.WorkWeiXinAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.Push.WorkWeiXinAgent.Interfaces
{
    public interface IWorkWeiXinPushApi
    {
        [Headers("Content-Type:application/json; charset=UTF-8")]
        [Post("/cgi-bin/webhook/send?key=1d919c89-5c17-4970-91ac-82f63a672700")]
        Task Send([Body] WorkWeiXinPushRequest request);
    }
}
