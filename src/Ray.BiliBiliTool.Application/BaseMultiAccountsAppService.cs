using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public abstract class BaseMultiAccountsAppService(
    ILogger logger,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : AppService
{
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("账号数：{count}", cookieStrFactory.Count);
        for (int i = 0; i < cookieStrFactory.Count; i++)
        {
            logger.LogInformation(
                "######### 账号 {num} #########{newLine}",
                i,
                Environment.NewLine
            );
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
}
