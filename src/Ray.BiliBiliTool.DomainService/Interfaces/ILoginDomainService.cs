using System.Threading;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 账户
    /// </summary>
    public interface ILoginDomainService : IDomainService
    {
        /// <summary>
        /// 扫描二维码登录
        /// </summary>
        /// <returns></returns>
        Task<BiliCookie> LoginByQrCodeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Set Cookie
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        Task<BiliCookie> SetCookieAsync(BiliCookie cookie, CancellationToken cancellationToken);

        /// <summary>
        /// 持久化Cookie到配置文件
        /// </summary>
        /// <returns></returns>
        Task SaveCookieToJsonFileAsync(BiliCookie ckInfo, CancellationToken cancellationToken);

        /// <summary>
        /// 持久化Cookie到青龙环境变量
        /// </summary>
        /// <returns></returns>
        Task SaveCookieToQinLongAsync(BiliCookie ckInfo, CancellationToken cancellationToken);
    }
}
