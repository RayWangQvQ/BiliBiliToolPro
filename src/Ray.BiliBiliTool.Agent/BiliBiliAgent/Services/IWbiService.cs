using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

/// <summary>
/// 防爬
/// </summary>
public interface IWbiService
{
    Task<WridDto> GetWridAsync(Dictionary<string, string> parameters, BiliCookie ck);

    /// <summary>
    /// 获取WbiKey
    /// </summary>
    /// <returns></returns>
    Task SetWridAsync<T>(T ob, BiliCookie ck)
        where T : IWrid;

    WridDto EncWbi(
        Dictionary<string, string> parameters,
        string imgKey,
        string subKey,
        long timespan = 0
    );
}
