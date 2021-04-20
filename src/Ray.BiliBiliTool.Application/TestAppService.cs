using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class TestAppService : AppService, ITestAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAccountDomainService _accountDomainService;

        public TestAppService(
            IConfiguration configuration,
            ILogger<LiveLotteryTaskAppService> logger,
            IAccountDomainService accountDomainService
            )
        {
            _configuration = configuration;
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        [TaskInterceptor("测试Cookie")]
        public override void DoTask()
        {
            _accountDomainService.LoginByCookie();
        }
    }
}
