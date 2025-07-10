using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

public class MangaTaskAppService(
    ILogger<MangaTaskAppService> logger,
    IAccountDomainService accountDomainService,
    IMangaDomainService mangaDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : BaseMultiAccountsAppService(logger, cookieStrFactory), IMangaTaskAppService
{
    [TaskInterceptor("漫画任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await SetCookiesAsync(ck, cancellationToken);
        await Login(ck);

        await MangaSign(ck);
        await MangaRead(ck);
    }

    [TaskInterceptor("Set Cookie")]
    private async Task SetCookiesAsync(BiliCookie biliCookie, CancellationToken cancellationToken)
    {
        //判断cookie是否完整
        if (biliCookie.Buvid.IsNotNullOrEmpty())
        {
            logger.LogInformation("Cookie完整，不需要Set Cookie");
            return;
        }

        //Set
        logger.LogInformation("开始Set Cookie");
        var ck = await loginDomainService.SetCookieAsync(biliCookie, cancellationToken);

        //持久化
        logger.LogInformation("持久化Cookie");
        await SaveCookieAsync(ck, cancellationToken);
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <returns></returns>
    [TaskInterceptor("登录")]
    private async Task Login(BiliCookie ck)
    {
        await accountDomainService.LoginByCookie(ck);
    }

    /// <summary>
    /// 漫画签到
    /// </summary>
    [TaskInterceptor("漫画签到", rethrowWhenException: false)]
    private async Task MangaSign(BiliCookie ck)
    {
        await mangaDomainService.MangaSign(ck);
    }

    /// <summary>
    /// 漫画阅读
    /// </summary>
    [TaskInterceptor("漫画阅读", rethrowWhenException: false)]
    private async Task MangaRead(BiliCookie ck)
    {
        await mangaDomainService.MangaRead(ck);
    }

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
