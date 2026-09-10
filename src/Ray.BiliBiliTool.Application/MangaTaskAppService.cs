using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Diagnostics;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class MangaTaskAppService(
    ILogger<MangaTaskAppService> logger,
    IOptionsMonitor<MangaTaskOptions> mangaTaskOptions,
    IAccountDomainService accountDomainService,
    IMangaDomainService mangaDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
)
    : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration),
        IMangaTaskAppService
{
    [TaskInterceptor("漫画任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "漫画任务",
            async () =>
            {
                if (!mangaTaskOptions.CurrentValue.IsEnable)
                {
                    logger.LogInformation("已配置为关闭，跳过");
                    return;
                }

                await SetCookiesAsync(ck, cancellationToken);
                await Login(ck);

                await MangaSign(ck);
                await MangaRead(ck);
            }
        );
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
}
