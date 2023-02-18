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
        private readonly ILogger<LiveFansMedalAppService> _logger;
        private readonly ILiveDomainService _liveDomainService;

        public LiveFansMedalAppService(
            ILiveDomainService liveDomainService,
            ILogger<LiveFansMedalAppService> logger
            )
        {
            _liveDomainService = liveDomainService;
            _logger = logger;
        }

        [TaskInterceptor("直播间互动", TaskLevel.One)]
        public override Task DoTaskAsync(CancellationToken cancellationToken)
        {
            SendDanmaku();
            Like();
            HeartBeat();
            return Task.CompletedTask;
        }

        [TaskInterceptor("发送弹幕", TaskLevel.Two,false)]
        private void SendDanmaku()
        {
            _liveDomainService.SendDanmakuToFansMedalLive();
        }

        [TaskInterceptor("点赞直播间", TaskLevel.Two,false)]
        private void Like()
        {
            _liveDomainService.LikeFansMedalLive();
        }

        [TaskInterceptor("直播时长挂机", TaskLevel.Two,false)]
        private void HeartBeat()
        {
            _liveDomainService.SendHeartBeatToFansMedalLive();
        }
    }
}
