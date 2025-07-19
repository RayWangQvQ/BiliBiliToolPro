using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

public class VipPrivilegeTaskAppService(
    ILogger<VipPrivilegeTaskAppService> logger,
    IOptionsMonitor<VipPrivilegeOptions> vipPrivilegeOptions,
    IAccountDomainService accountDomainService,
    IVipPrivilegeDomainService vipPrivilegeDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : BaseMultiAccountsAppService(logger, cookieStrFactory), IVipPrivilegeTaskAppService
{
    [TaskInterceptor("领取大会员福利任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        if (!vipPrivilegeOptions.CurrentValue.IsEnable)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return;
        }

        await SetCookiesAsync(ck, cancellationToken);
        UserInfo userInfo = await Login(ck);

        await ReceiveVipPrivilege(userInfo, ck);
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
    private async Task<UserInfo> Login(BiliCookie ck)
    {
        UserInfo userInfo = await accountDomainService.LoginByCookie(ck);
        return userInfo;
    }

    /// <summary>
    /// 每月领取大会员福利
    /// </summary>
    [TaskInterceptor("领取", rethrowWhenException: false)]
    private async Task ReceiveVipPrivilege(UserInfo userInfo, BiliCookie ck)
    {
        var suc = await vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo, ck);

        //如果领取成功，需要刷新账户信息（比如B币余额）
        if (suc)
        {
            try
            {
                await accountDomainService.LoginByCookie(ck);
            }
            catch (Exception ex)
            {
                logger.LogError("领取福利成功，但之后刷新用户信息时异常，信息：{msg}", ex.Message);
            }
        }
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
