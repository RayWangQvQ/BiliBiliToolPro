using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

/// <summary>
/// 防爬
/// </summary>
public interface IWbiService
{
    /// <summary>
    /// 获取WbiKey
    /// </summary>
    /// <returns></returns>
    Task SetWridAsync<T>(T ob) where T : IWrid;

    WridDto EncWbi(Dictionary<string, object> parameters, string imgKey, string subKey, long timespan = 0);
}
