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

public class MangaPrivilegeTaskAppService(
    ILogger<MangaPrivilegeTaskAppService> logger,
    IOptionsMonitor<MangaPrivilegeTaskOptions> mangaPrivilegeTaskOptions,
    IAccountDomainService accountDomainService,
    IMangaDomainService mangaDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
)
    : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration),
        IMangaPrivilegeTaskAppService
{
    [TaskInterceptor("每月领取大会员漫画权益任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "漫画权益任务",
            async () =>
            {
                if (!mangaPrivilegeTaskOptions.CurrentValue.IsEnable)
                {
                    logger.LogInformation("已配置为关闭，跳过");
                    return;
                }

                await SetCookiesAsync(ck, cancellationToken);
                UserInfo userInfo = await Login(ck);
                await ReceiveMangaVipReward(userInfo, ck);
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
    /// 每月获取大会员漫画权益
    /// </summary>
    [TaskInterceptor("领取大会员漫画权益", rethrowWhenException: false)]
    private async Task ReceiveMangaVipReward(UserInfo userInfo, BiliCookie ck)
    {
        await mangaDomainService.ReceiveMangaVipReward(1, userInfo, ck);
    }
}
