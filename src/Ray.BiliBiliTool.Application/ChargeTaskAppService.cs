using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Diagnostics;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class ChargeTaskAppService(
    ILogger<ChargeTaskAppService> logger,
    IOptionsMonitor<ChargeTaskOptions> chargeTaskOptions,
    IAccountDomainService accountDomainService,
    IChargeDomainService chargeDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
)
    : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration),
        IChargeTaskAppService
{
    [TaskInterceptor("免费B币券充电任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "充电任务",
            async () =>
            {
                if (!chargeTaskOptions.CurrentValue.IsEnable)
                {
                    logger.LogInformation("已配置为关闭，跳过");
                    return;
                }

                await SetCookiesAsync(ck, cancellationToken);
                UserInfo userInfo = await Login(ck);
                await Charge(userInfo, ck);
            }
        );
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
    /// 每月为自己充电
    /// </summary>
    [TaskInterceptor("B币券充电", rethrowWhenException: false)]
    private async Task Charge(UserInfo userInfo, BiliCookie ck)
    {
        await chargeDomainService.Charge(userInfo, ck);
    }
}
