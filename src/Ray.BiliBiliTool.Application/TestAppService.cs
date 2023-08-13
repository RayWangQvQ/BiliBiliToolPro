using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application
{
    public class TestAppService : AppService, ITestAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly CookieStrFactory _cookieStrFactory;
        private readonly IConfiguration _configuration;
        private readonly IAccountDomainService _accountDomainService;

        public TestAppService(
            IConfiguration configuration,
            ILogger<LiveLotteryTaskAppService> logger,
            CookieStrFactory cookieStrFactory,
            IAccountDomainService accountDomainService
            )
        {
            _configuration = configuration;
            _logger = logger;
            _cookieStrFactory = cookieStrFactory;
            _accountDomainService = accountDomainService;
        }

        [TaskInterceptor("测试Cookie")]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < _cookieStrFactory.Count; i++)
            {
                _cookieStrFactory.CurrentNum = i + 1;
                _logger.LogInformation("######### 账号 {num} #########{newLine}", _cookieStrFactory.CurrentNum, Environment.NewLine);

                try
                {
                    await DoEachAccountAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    //ignore
                    _logger.LogWarning("异常：{msg}", e);
                }
            }
            _logger.LogInformation("·开始推送·{task}·{user}", "Test任务", "");
        }

        protected async Task DoEachAccountAsync(CancellationToken cancellationToken)
        {
            await _accountDomainService.LoginByCookie();
        }
    }
}
