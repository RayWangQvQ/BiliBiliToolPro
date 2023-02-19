using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class LiveFansMedalAppService : AppService, ILiveFansMedalAppService
    {
        private readonly ILiveDomainService _liveDomainService;

        public LiveFansMedalAppService(
            ILiveDomainService liveDomainService
            )
        {
            _liveDomainService = liveDomainService;
        }

        [TaskInterceptor("直播间互动", TaskLevel.One)]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            await SendDanmaku();
            await Like();
            await HeartBeat();
        }

        [TaskInterceptor("发送弹幕", TaskLevel.Two, false)]
        private async Task SendDanmaku()
        {
            await _liveDomainService.SendDanmakuToFansMedalLive();
        }

        [TaskInterceptor("点赞直播间", TaskLevel.Two, false)]
        private async Task Like()
        {
            await _liveDomainService.LikeFansMedalLive();
        }

        [TaskInterceptor("直播时长挂机", TaskLevel.Two, false)]
        private async Task HeartBeat()
        {
            await _liveDomainService.SendHeartBeatToFansMedalLive();
        }
    }
}
