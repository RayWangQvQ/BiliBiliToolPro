using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 主站首页接口API
    /// </summary>
    public interface IHomeApi : IBiliBiliApi
    {
        [HttpGet("")]
        Task<HttpResponseMessage> GetHomePageAsync([Header("Cookie")] string ck);
    }
}
