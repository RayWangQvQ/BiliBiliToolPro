using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class TestAppService : MultiAccountsAppService, ITestAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly BiliCookieContainer _biliCookieContainer;
        private readonly IAccountDomainService _accountDomainService;

        public TestAppService(
            ILogger<LiveLotteryTaskAppService> logger,
            BiliCookieContainer biliCookieContainer,
            IAccountDomainService accountDomainService
            ):base(logger, biliCookieContainer, accountDomainService)
        {
            _logger = logger;
            _biliCookieContainer = biliCookieContainer;
            _accountDomainService = accountDomainService;
        }

        protected override async Task DoEachAccountAsync(CancellationToken cancellationToken)
        {
            await _accountDomainService.LoginByCookie();
        }
    }
}
