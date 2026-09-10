using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 主站首页接口API
/// </summary>
public interface IHomeApi
{
    [Get("")]
    Task<HttpResponseMessage> GetHomePageAsync([Header("Cookie")] string ck);
}
