using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Application
{
    public class LiveFansMedalAppService : AppService, ILiveFansMedalAppService
    {
        private readonly ILogger<LiveFansMedalAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILiveDomainService _liveDomainService;
        private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions;
        private readonly SecurityOptions _securityOptions;
        private readonly IAccountDomainService _accountDomainService;

        public LiveFansMedalAppService(
            IConfiguration configuration,
            ILiveDomainService liveDomainService,
            IOptionsMonitor<SecurityOptions> securityOptions,
            IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions,
            ILogger<LiveFansMedalAppService> logger,
            IAccountDomainService accountDomainService
            )
        {
            _configuration = configuration;
            _liveDomainService = liveDomainService;
            _liveLotteryTaskOptions = liveLotteryTaskOptions.CurrentValue;
            _securityOptions = securityOptions.CurrentValue;
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        [TaskInterceptor("直播间互动", TaskLevel.One)]
        public override void DoTask()
        {
            _liveDomainService.SendDanmakuToFansMedalLive();
            _liveDomainService.SendHeartBeatToFansMdealLive();
        }

    }
}
