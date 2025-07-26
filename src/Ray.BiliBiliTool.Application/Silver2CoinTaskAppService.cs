using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

public class Silver2CoinTaskAppService(
    ILogger<Silver2CoinTaskAppService> logger,
    IOptionsMonitor<Silver2CoinTaskOptions> silver2CoinTaskOptions,
    IAccountDomainService accountDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    ILiveDomainService liveDomainService,
    ICoinDomainService coinDomainService,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : BaseMultiAccountsAppService(logger, cookieStrFactory), ISilver2CoinTaskAppService
{
    [TaskInterceptor("银瓜子兑换硬币任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        if (!silver2CoinTaskOptions.CurrentValue.IsEnable)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return;
        }

        await SetCookiesAsync(ck, cancellationToken);
        await Login(ck);

        await ExchangeSilver2Coin(ck);
    }

    [TaskInterceptor("Set Cookie")]
    private async Task SetCookiesAsync(BiliCookie biliCookie, CancellationToken cancellationToken)
    {
        //判断cookie是否完整
        if (!string.IsNullOrWhiteSpace(biliCookie.Buvid))
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
    /// 直播中心的银瓜子兑换硬币
    /// </summary>
    [TaskInterceptor("银瓜子兑换硬币", rethrowWhenException: false)]
    private async Task ExchangeSilver2Coin(BiliCookie ck)
    {
        var success = await liveDomainService.ExchangeSilver2Coin(ck);
        if (!success)
            return;

        //如果兑换成功，则打印硬币余额
        var coinBalance = coinDomainService.GetCoinBalance(ck);
        logger.LogInformation("【硬币余额】 {coin}", coinBalance);
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
