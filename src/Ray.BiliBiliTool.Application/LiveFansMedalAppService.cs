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

public class LiveFansMedalAppService(
    ILogger<LiveFansMedalAppService> logger,
    ILiveDomainService liveDomainService,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : BaseMultiAccountsAppService(logger, cookieStrFactory), ILiveFansMedalAppService
{
    [TaskInterceptor("直播间互动", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await SendDanmaku(ck);
        await Like(ck);
        await HeartBeat(ck);
    }

    [TaskInterceptor("发送弹幕", TaskLevel.Two, false)]
    private async Task SendDanmaku(BiliCookie ck)
    {
        await liveDomainService.SendDanmakuToFansMedalLive(ck);
    }

    [TaskInterceptor("点赞直播间", TaskLevel.Two, false)]
    private async Task Like(BiliCookie ck)
    {
        await liveDomainService.LikeFansMedalLive(ck);
    }

    [TaskInterceptor("直播时长挂机", TaskLevel.Two, false)]
    private async Task HeartBeat(BiliCookie ck)
    {
        await liveDomainService.SendHeartBeatToFansMedalLive(ck);
    }
}
