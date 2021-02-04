using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 用户信息接口API
    /// </summary>
    public interface IUserInfoApi : IBiliBiliApi
    {
        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [Get("/x/web-interface/nav")]
        Task<BiliApiResponse<UserInfo>> LoginByCookie();
    }
}
