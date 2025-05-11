using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class TestAppService(
    IConfiguration configuration,
    ILogger<TestAppService> logger,
    IAccountDomainService accountDomainService,
    CookieStrFactory cookieStrFactory
) : AppService, ITestAppService
{
    [TaskInterceptor("测试Cookie")]
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
                await accountDomainService.LoginByCookie();
            }
            catch (Exception e)
            {
                //ignore
                logger.LogWarning("异常：{msg}", e);
            }
        }
    }
}
