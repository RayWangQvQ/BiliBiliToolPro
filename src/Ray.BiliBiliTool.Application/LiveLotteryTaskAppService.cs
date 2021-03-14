using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class LiveLotteryTaskAppService : AppService, ILiveLotteryTaskAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly ILiveDomainService _liveDomainService;
        private readonly SecurityOptions _securityOptions;
        private readonly IAccountDomainService _accountDomainService;

        public LiveLotteryTaskAppService(
            ILiveDomainService liveDomainService,
            IOptionsMonitor<SecurityOptions> securityOptions,
            ILogger<LiveLotteryTaskAppService> logger,
            IAccountDomainService accountDomainService
            )
        {
            _liveDomainService = liveDomainService;
            _securityOptions = securityOptions.CurrentValue;
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        public override string TaskName => "LiveLottery";


        public override void DoTask()
        {
            LogUserInfo();
            LotteryTianXuan();
        }

        [TaskInterceptor("打印用户信息")]
        private void LogUserInfo()
        {
            UserInfo userInfo = _accountDomainService.LoginByCookie();
        }

        [TaskInterceptor("天选时刻抽奖")]
        private void LotteryTianXuan()
        {
            _liveDomainService.TianXuan();
        }
    }
}
