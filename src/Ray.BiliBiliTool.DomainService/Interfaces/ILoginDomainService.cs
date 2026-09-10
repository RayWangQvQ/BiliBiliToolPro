using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.DomainService.Dtos;


namespace Ray.BiliBiliTool.DomainService.Interfaces;

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
    /// 生成二维码（Web端专用，返回PNG图片base64）
    /// </summary>
    Task<QrLoginGenerateResult> GenerateQrCodeWebAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 检测二维码扫描状态（Web端专用）
    /// </summary>
    Task<QrLoginCheckResult> CheckQrLoginAsync(
        string qrcodeKey,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 持久化Cookie到配置文件
    /// </summary>
    /// <returns></returns>
    Task SaveCookieToJsonFileAsync(BiliCookie ckInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 持久化Cookie到青龙环境变量
    /// </summary>
    /// <param name="ckInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> SaveCookieToQinLongAsync(BiliCookie ckInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 持久化Cookie到白虎环境变量
    /// </summary>
    /// <param name="ckInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> SaveCookieToBaihuAsync(BiliCookie ckInfo, CancellationToken cancellationToken);
}
