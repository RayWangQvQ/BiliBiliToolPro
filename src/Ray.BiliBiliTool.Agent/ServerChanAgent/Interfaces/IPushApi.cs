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
        [Post("/{scKey}.send?text={title}&desp={content}")]
        Task<PushResponse> Send(string scKey, string title, string content);
    }
}
