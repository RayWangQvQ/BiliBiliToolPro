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
) : AppService, ILiveFansMedalAppService
{
    [TaskInterceptor("直播间互动", TaskLevel.One)]
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
                await SendDanmaku();
                await Like();
                await HeartBeat();
            }
            catch (Exception e)
            {
                //ignore
                logger.LogWarning("异常：{msg}", e);
            }
        }
    }

    [TaskInterceptor("发送弹幕", TaskLevel.Two, false)]
    private async Task SendDanmaku()
    {
        await liveDomainService.SendDanmakuToFansMedalLive();
    }

    [TaskInterceptor("点赞直播间", TaskLevel.Two, false)]
    private async Task Like()
    {
        await liveDomainService.LikeFansMedalLive();
    }

    [TaskInterceptor("直播时长挂机", TaskLevel.Two, false)]
    private async Task HeartBeat()
    {
        await liveDomainService.SendHeartBeatToFansMedalLive();
    }
}
