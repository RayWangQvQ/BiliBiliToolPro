using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

public abstract class BaseMultiAccountsAppService(
    ILogger logger,
    CookieStrFactory<BiliCookie> cookieStrFactory,
    ILoginDomainService loginDomainService,
    IConfiguration configuration
) : AppService
{
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "【账号个数】{count}个" + Environment.NewLine,
            cookieStrFactory.Count
        );
        for (int i = 0; i < cookieStrFactory.Count; i++)
        {
            logger.LogInformation("######### 账号 {num} #########" + Environment.NewLine, i);
            var ck = cookieStrFactory.GetCookie(i);
            try
            {
                await DoTaskAccountAsync(ck, cancellationToken);
            }
            catch (Exception e)
            {
                //ignore
                logger.LogWarning("异常：{msg}", e);
            }
        }
    }

    protected abstract Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    );

    protected virtual async Task SetCookiesAsync(
        BiliCookie biliCookie,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(biliCookie.Buvid))
        {
            logger.LogInformation("Cookie完整，不需要Set Cookie");
            return;
        }

        logger.LogInformation("开始Set Cookie");
        var ck = await loginDomainService.SetCookieAsync(biliCookie, cancellationToken);

        logger.LogInformation("持久化Cookie");
        await SaveCookieAsync(ck, cancellationToken);
    }

    protected virtual async Task SaveCookieAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        var platformType = configuration.GetSection("PlatformType").Get<PlatformType>();
        logger.LogInformation("当前运行平台：{platform}", platformType);

        if (platformType == PlatformType.QingLong)
        {
            await loginDomainService.SaveCookieToQinLongAsync(ckInfo, cancellationToken);
            return;
        }

        //更新cookie到白虎env
        if (platformType == PlatformType.Baihu)
        {
            await loginDomainService.SaveCookieToBaihuAsync(ckInfo, cancellationToken);
            return;
        }

        await loginDomainService.SaveCookieToJsonFileAsync(ckInfo, cancellationToken);
    }
}
