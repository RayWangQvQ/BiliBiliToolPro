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
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application
{
    public class TestAppService : AppService, ITestAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IAccountDomainService _accountDomainService;
        private readonly BiliCookie _biliCookie;

        public TestAppService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<LiveLotteryTaskAppService> logger,
            IAccountDomainService accountDomainService
            //BiliCookie biliCookie
            )
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _accountDomainService = accountDomainService;
            //_biliCookie = biliCookie;
        }

        [TaskInterceptor("测试Cookie")]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            var ckStrList = _configuration.GetSection("BiliBiliCookies")
                .Get<List<string>>() ?? new List<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            for (int i = 0; i < ckStrList.Count; i++)
            {
                _logger.LogInformation("######### 账号 {num} #########{newLine}", i+1, Environment.NewLine);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();
                    biliCookie.Init(ckStrList[i]);
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
