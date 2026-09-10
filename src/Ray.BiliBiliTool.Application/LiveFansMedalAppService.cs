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

public class LiveFansMedalAppService(
    ILogger<LiveFansMedalAppService> logger,
    IOptionsMonitor<LiveFansMedalTaskOptions> liveFansMedalTaskOptions,
    ILiveDomainService liveDomainService,
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
)
    : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration),
        ILiveFansMedalAppService
{
    [TaskInterceptor("直播间互动", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "直播间互动",
            async () =>
            {
                if (!liveFansMedalTaskOptions.CurrentValue.IsEnable)
                {
                    logger.LogInformation("已配置为关闭，跳过");
                    return;
                }

                await SetCookiesAsync(ck, cancellationToken);
                await SendDanmaku(ck);
                await Like(ck);
                await HeartBeat(ck);
            }
        );
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
