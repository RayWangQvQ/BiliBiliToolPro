using Ray.BiliBiliTool.DomainService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;

namespace Ray.BiliBiliTool.Application.Contracts
{
    public abstract class EveryAccountAppService: AppService,IAppService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IAccountDomainService _accountDomainService;

        protected EveryAccountAppService(
            IServiceProvider serviceProvider,
            ILogger logger,
            IAccountDomainService accountDomainService
            )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
        {
            var ckStrList = await _accountDomainService.GetAllCookieStrListAsync();
            for (int i = 0; i < ckStrList.Count; i++)
            {
                _logger.LogInformation("######### 账号 {num} #########{newLine}", i + 1, Environment.NewLine);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    BiliCookie biliCookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();
                    biliCookie.Init(ckStrList[i]);
                    await DoEachAccountAsync(biliCookie, cancellationToken);
                }
                catch (Exception e)
                {
                    //ignore
                    _logger.LogWarning("异常：{msg}", e);
                }
            }
            _logger.LogInformation("·开始推送·{task}·{user}", $"{this.Description()}任务", "");
        }

        protected abstract Task DoEachAccountAsync(BiliCookie biliCookie, CancellationToken cancellationToken);
    }
}
