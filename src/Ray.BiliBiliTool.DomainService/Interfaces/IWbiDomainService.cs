using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 账户
    /// </summary>
    public interface IWbiDomainService : IDomainService
    {
        /// <summary>
        /// 获取WbiKey
        /// </summary>
        /// <returns></returns>
        Task<WridDto> GetWridAsync(object ob);

        Task<WbiImg> GetWbiKeysAsync();

        string GetMixinKey(string orig);

        WridDto EncWbi(Dictionary<string, object> parameters, string imgKey, string subKey, long timespan = 0);
    }
}
