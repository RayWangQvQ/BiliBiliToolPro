using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class UnfollowBatchedTaskAppService(
    ILogger<UnfollowBatchedTaskAppService> logger,
    IAccountDomainService accountDomainService,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : AppService, IUnfollowBatchedTaskAppService
{
    [TaskInterceptor("批量取关", TaskLevel.One)]
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("账号数：{count}", cookieStrFactory.Count);
        for (int i = 0; i < cookieStrFactory.Count; i++)
        {
            cookieStrFactory.CurrentNum = i + 1;
            logger.LogInformation(
                "######### 账号 {num} #########{newLine}",
                cookieStrFactory.CurrentNum,
                Environment.NewLine
            );

            try
            {
                await accountDomainService.UnfollowBatched();
            }
            catch (Exception e)
            {
                //ignore
                logger.LogWarning("异常：{msg}", e);
            }
        }
    }
}
