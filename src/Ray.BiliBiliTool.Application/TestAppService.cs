using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class TestAppService : EveryAccountAppService, ITestAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly IAccountDomainService _accountDomainService;

        public TestAppService(
            IServiceProvider serviceProvider,
            ILogger<LiveLotteryTaskAppService> logger,
            IAccountDomainService accountDomainService
            ):base(serviceProvider, logger, accountDomainService)
        {
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        protected override async Task DoEachAccountAsync(BiliCookie biliCookie, CancellationToken cancellationToken)
        {
            await _accountDomainService.LoginByCookie();
        }
    }
}
