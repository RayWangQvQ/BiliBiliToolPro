using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.ServerChanAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.ServerChanAgent.Interfaces
{
    public interface IPushApi
    {
        [Headers("Content-Type:application/x-www-form-urlencoded; charset=UTF-8",
            "Accept:zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6",
            "X-Requested-With:XMLHttpRequest")]
        [Post("/{scKey}.send")]
        Task<PushResponse> Send(string scKey, [Body(BodySerializationMethod.UrlEncoded)] PushRequest request);
    }
}
