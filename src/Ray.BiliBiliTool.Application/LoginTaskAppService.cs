using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

public class LoginTaskAppService(
    IConfiguration configuration,
    ILogger<LoginTaskAppService> logger,
    ILoginDomainService loginDomainService
) : AppService, ILoginTaskAppService
{
    [TaskInterceptor("扫码登录", TaskLevel.One)]
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        //扫码登录
        var cookieInfo = await QrCodeLoginAsync(cancellationToken);
        if (cookieInfo == null)
            return;

        //set cookie
        cookieInfo = await SetCookiesAsync(cookieInfo, cancellationToken);

        //持久化cookie
        await SaveCookieAsync(cookieInfo, cancellationToken);
    }

    [TaskInterceptor("获取二维码")]
    private async Task<BiliCookie> QrCodeLoginAsync(CancellationToken cancellationToken)
    {
        var biliCookie = await loginDomainService.LoginByQrCodeAsync(cancellationToken);
        return biliCookie;
    }

    [TaskInterceptor("Set Cookie")]
    private async Task<BiliCookie> SetCookiesAsync(
        BiliCookie biliCookie,
        CancellationToken cancellationToken
    )
    {
        var ck = await loginDomainService.SetCookieAsync(biliCookie, cancellationToken);
        return ck;
    }

    [TaskInterceptor("持久化Cookie")]
    private async Task SaveCookieAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
    {
        var platformType = configuration.GetSection("PlatformType").Get<PlatformType>();
        logger.LogInformation("当前运行平台：{platform}", platformType);

        //更新cookie到青龙env
        if (platformType == PlatformType.QingLong)
        {
            await loginDomainService.SaveCookieToQinLongAsync(ckInfo, cancellationToken);
            return;
        }

        //更新cookie到json
        await loginDomainService.SaveCookieToJsonFileAsync(ckInfo, cancellationToken);
    }
}
