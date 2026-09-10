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

public class UnfollowBatchedTaskAppService(
    ILogger<UnfollowBatchedTaskAppService> logger,
    IOptionsMonitor<UnfollowBatchedTaskOptions> unfollowBatchedTaskOptions,
    IAccountDomainService accountDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
)
    : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration),
        IUnfollowBatchedTaskAppService
{
    [TaskInterceptor("批量取关", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "批量取关",
            async () =>
            {
                if (!unfollowBatchedTaskOptions.CurrentValue.IsEnable)
                {
                    logger.LogInformation("已配置为关闭，跳过");
                    return;
                }

                await SetCookiesAsync(ck, cancellationToken);
                await accountDomainService.UnfollowBatched(ck);
            }
        );
    }
}
