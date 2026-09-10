using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 导航接口API（api.bilibili.com/x/web-interface/nav）
/// </summary>
[Headers(
    "Referer: https://www.bilibili.com/",
    "Origin: https://www.bilibili.com",
    "Host: api.bilibili.com"
)]
public interface INavApi
{
    /// <summary>
    /// 获取导航栏信息（含登录态、用户信息、Wbi 密钥）
    /// </summary>
    /// <returns></returns>
    [Get("/x/web-interface/nav")]
    Task<BiliApiResponse<UserInfo>> GetNavAsync([Header("Cookie")] string ck);
}
