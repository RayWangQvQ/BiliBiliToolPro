using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class VipBigPointAppService(
    ILogger<VipBigPointAppService> logger,
    IAccountDomainService loginDomainService,
    IVipBigPointDomainService vipBigPointDomainService,
    CookieStrFactory<BiliCookie> cookieFactory
) : BaseMultiAccountsAppService(logger, cookieFactory), IVipBigPointAppService
{
    [TaskInterceptor("大会员大积分", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        bool isVip = await LoginAndCheckVipStatusAsync(ck, cancellationToken);
        if (!isVip)
        {
            return;
        }

        await ExpressAsync(ck, cancellationToken);
        await SignAsync(ck, cancellationToken);
        var combine = await CheckCombineAsync(ck, cancellationToken);

        // 2 个一次性任务
        await BonusMissionAsync(combine, ck, cancellationToken);
        await PrivilegeMissionAsync(combine, ck, cancellationToken);

        // 日常任务
        await ReceiveMissionsAsync(combine, ck, cancellationToken);
        await DailyMissionsAsync(combine, ck, cancellationToken);

        await CheckCombineAsync(ck, cancellationToken);
    }

    [TaskInterceptor("登录并检测会员状态")]
    private async Task<bool> LoginAndCheckVipStatusAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        UserInfo userInfo = await loginDomainService.LoginByCookie(ck);
        if (userInfo.GetVipType() == VipType.None)
        {
            logger.LogInformation("当前不是大会员，跳过任务");
            return false;
        }

        return true;
    }

    [TaskInterceptor("查看大会员大积分状态")]
    private async Task<VipBigPointCombine> CheckCombineAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        VipBigPointCombine combine = await vipBigPointDomainService.GetCombineAsync(ck);
        combine.LogFullInfo(logger);
        return combine;
    }

    /// <summary>
    /// 领经验（专属等级加速包），观看视频 1 分钟领取 10 经验
    /// </summary>
    /// <param name="ck"></param>
    /// <param name="cancellationToken"></param>
    [TaskInterceptor("大会员经验观看任务", rethrowWhenException: false)]
    private async Task ExpressAsync(BiliCookie ck, CancellationToken cancellationToken = default)
    {
        await vipBigPointDomainService.VipExpressAsync(ck);
    }

    [TaskInterceptor("签到任务", rethrowWhenException: false)]
    private async Task SignAsync(BiliCookie ck, CancellationToken cancellationToken = default)
    {
        await vipBigPointDomainService.SignAsync(ck);
    }

    [TaskInterceptor("领取日常任务", rethrowWhenException: false)]
    private async Task ReceiveMissionsAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveDailyMissionsAsync(combine, ck);
    }

    [TaskInterceptor("福利任务", rethrowWhenException: false)]
    private async Task BonusMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "福利任务",
            "bonus",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteAsync("bonus", ck)
        );
    }

    [TaskInterceptor("体验任务", rethrowWhenException: false)]
    private async Task PrivilegeMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "体验任务",
            "privilege",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteAsync("privilege", ck)
        );
    }

    [TaskInterceptor("日常任务", rethrowWhenException: false)]
    private async Task DailyMissionsAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await DailyDressViewMissionAsync(combine, ck, cancellationToken);
        await DailyVipMallViewMissionAsync(combine, ck, cancellationToken);
        await DailyVipMallBuyMissionAsync(cancellationToken);
        await DailyAnimateTabMissionAsync(combine, ck, cancellationToken);
        await DailyFilmTabMissionAsync(combine, ck, cancellationToken);
        await DailyOgvWatchMissionAsync(combine, ck, cancellationToken);
        await DailyTvOdBuyMissionAsync(cancellationToken);
        await DailyDressBuyAmountMissionAsync(cancellationToken);
    }

    [TaskInterceptor("日常1：浏览装扮商城", TaskLevel.Three, rethrowWhenException: false)]
    private async Task DailyDressViewMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "日常任务",
            "dress-view",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteV2Async("dress-view", ck)
        );
    }

    [TaskInterceptor("日常2：浏览会员购", TaskLevel.Three, rethrowWhenException: false)]
    private async Task DailyVipMallViewMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "日常任务",
            "vipmallview",
            ck,
            async (_, _) =>
                await vipBigPointDomainService.CompleteViewVipMallAsync("vipmallview", ck)
        );
    }

    [TaskInterceptor("日常3：购买会员购", TaskLevel.Three, rethrowWhenException: false)]
    private Task DailyVipMallBuyMissionAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("需购买，跳过");
        return Task.CompletedTask;
    }

    [TaskInterceptor("日常4：浏览追番频道", TaskLevel.Three, rethrowWhenException: false)]
    private async Task DailyAnimateTabMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "日常任务",
            "animatetab",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteViewAsync("animatetab", ck)
        );
    }

    [TaskInterceptor("日常5：浏览影视频道", TaskLevel.Three, rethrowWhenException: false)]
    private async Task DailyFilmTabMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "日常任务",
            "filmtab",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteViewAsync("filmtab", ck)
        );
    }

    [TaskInterceptor("日常6：观看剧集", TaskLevel.Three, rethrowWhenException: false)]
    private async Task DailyOgvWatchMissionAsync(
        VipBigPointCombine combine,
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            combine,
            "日常任务",
            "ogvwatchnew",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteV2Async("ogvwatchnew", ck)
        );
    }

    [TaskInterceptor("日常7：购买影片", TaskLevel.Three, rethrowWhenException: false)]
    private Task DailyTvOdBuyMissionAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("需购买，跳过");
        return Task.CompletedTask;
    }

    [TaskInterceptor("日常8：购买装扮", TaskLevel.Three, rethrowWhenException: false)]
    private Task DailyDressBuyAmountMissionAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("需购买，跳过");
        return Task.CompletedTask;
    }
}
