using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application
{
    public class UnfollowBatchedTaskAppService : AppService, IUnfollowBatchedTaskAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SecurityOptions _securityOptions;
        private readonly UnfollowBatchedTaskOptions _unfollowBatchedTaskOptions;
        private readonly IAccountDomainService _accountDomainService;

        public UnfollowBatchedTaskAppService(
            IConfiguration configuration,
            IOptionsMonitor<SecurityOptions> securityOptions,
            IOptionsMonitor<UnfollowBatchedTaskOptions> unfollwBatchedTaskOptions,
            ILogger<LiveLotteryTaskAppService> logger,
            IAccountDomainService accountDomainService
            )
        {
            _configuration = configuration;
            _securityOptions = securityOptions.CurrentValue;
            _logger = logger;
            _accountDomainService = accountDomainService;
            _unfollowBatchedTaskOptions = unfollwBatchedTaskOptions.CurrentValue;
        }

        [TaskInterceptor("批量取关", TaskLevel.One)]
        public override void DoTask()
        {
            _accountDomainService.UnfollowBatched(_unfollowBatchedTaskOptions.GroupName,
                _unfollowBatchedTaskOptions.Count);
        }
    }
}
