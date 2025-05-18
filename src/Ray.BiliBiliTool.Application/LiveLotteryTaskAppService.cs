using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
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
    CookieStrFactory<BiliCookie> cookieStrFactory
) : BaseMultiAccountsAppService(logger, cookieStrFactory), ILiveLotteryTaskAppService
{
    private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions =
        liveLotteryTaskOptions.CurrentValue;

    [TaskInterceptor("天选时刻抽奖", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await LogUserInfo(ck);
        await LotteryTianXuan(ck);
        await AutoGroupFollowings(ck);
    }

    [TaskInterceptor("打印用户信息")]
    private async Task LogUserInfo(BiliCookie ck)
    {
        await accountDomainService.LoginByCookie(ck);
    }

    [TaskInterceptor("抽奖")]
    private async Task LotteryTianXuan(BiliCookie ck)
    {
        await liveDomainService.TianXuan(ck);
    }

    [TaskInterceptor("自动分组关注的主播")]
    private async Task AutoGroupFollowings(BiliCookie ck)
    {
        if (_liveLotteryTaskOptions.AutoGroupFollowings)
        {
            await liveDomainService.GroupFollowing(ck);
        }
        else
        {
            logger.LogInformation("配置未开启，跳过");
        }
    }
}
