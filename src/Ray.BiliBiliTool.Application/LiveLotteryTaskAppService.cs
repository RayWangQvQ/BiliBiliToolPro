using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class LiveLotteryTaskAppService(
    ILiveDomainService liveDomainService,
    IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions,
    ILogger<LiveLotteryTaskAppService> logger,
    IAccountDomainService accountDomainService,
    CookieStrFactory cookieStrFactory
) : AppService, ILiveLotteryTaskAppService
{
    private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions =
        liveLotteryTaskOptions.CurrentValue;

    [TaskInterceptor("天选时刻抽奖", TaskLevel.One)]
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
                await LogUserInfo();
                await LotteryTianXuan();
                await AutoGroupFollowings();
            }
            catch (Exception e)
            {
                //ignore
                logger.LogWarning("异常：{msg}", e);
            }
        }
    }

    [TaskInterceptor("打印用户信息")]
    private async Task LogUserInfo()
    {
        await accountDomainService.LoginByCookie();
    }

    [TaskInterceptor("抽奖")]
    private async Task LotteryTianXuan()
    {
        await liveDomainService.TianXuan();
    }

    [TaskInterceptor("自动分组关注的主播")]
    private async Task AutoGroupFollowings()
    {
        if (_liveLotteryTaskOptions.AutoGroupFollowings)
        {
            await liveDomainService.GroupFollowing();
        }
        else
        {
            logger.LogInformation("配置未开启，跳过");
        }
    }
}
