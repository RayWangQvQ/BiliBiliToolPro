using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Dtos;
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
        var userInfo = await GetUserInfo(ck);
        if (userInfo.GetVipType() == VipType.None)
        {
            logger.LogInformation("当前不是大会员，跳过任务");
            return;
        }

        VipTaskInfo info = await vipBigPointDomainService.GetTaskListAsync(ck);
        info.LogInfo(logger);

        logger.LogInformation("大会员经验领取任务");
        await vipBigPointDomainService.VipExpressAsync(ck);

        //签到
        await vipBigPointDomainService.Sign(ck);

        //领取需要领取的任务
        await vipBigPointDomainService.ReceiveTasksAsync(info, ck);

        //福利任务
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            info,
            "福利任务",
            "bonus",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteAsync("bonus", ck)
        );

        //体验任务
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            info,
            "体验任务",
            "privilege",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteAsync("privilege", ck)
        );

        //日常任务
        //浏览追番频道页10秒
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            info,
            "日常任务",
            "animatetab",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteViewAsync("animatetab", ck)
        );

        //浏览会员购页面10秒
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            info,
            "日常任务",
            "vipmallview",
            ck,
            async (_, _) =>
                await vipBigPointDomainService.CompleteViewVipMallAsync("vipmallview", ck)
        );

        //浏览装扮商城
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            info,
            "日常任务",
            "dress-view",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteV2Async("dress-view", ck)
        );

        //观看剧集内容
        await vipBigPointDomainService.ReceiveAndCompleteAsync(
            info,
            "日常任务",
            "ogvwatchnew",
            ck,
            async (_, _) => await vipBigPointDomainService.CompleteV2Async("ogvwatchnew", ck)
        );

        info.LogInfo(logger);
    }

    [TaskInterceptor("测试Cookie")]
    private async Task<UserInfo> GetUserInfo(BiliCookie ck)
    {
        UserInfo userInfo = await loginDomainService.LoginByCookie(ck);

        return userInfo;
    }
}
