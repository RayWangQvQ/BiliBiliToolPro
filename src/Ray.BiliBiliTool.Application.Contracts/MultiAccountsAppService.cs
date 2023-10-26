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
    public abstract class MultiAccountsAppService: AppService,IAppService
    {
        private readonly ILogger _logger;
        private readonly BiliCookieContainer _biliCookieContainer;
        private readonly IAccountDomainService _accountDomainService;

        protected MultiAccountsAppService(
            ILogger logger,
            BiliCookieContainer biliCookieContainer,
            IAccountDomainService accountDomainService
            )
        {
            _logger = logger;
            _biliCookieContainer = biliCookieContainer;
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
                    _biliCookieContainer.Init(ckStrList[i]);
                    await DoEachAccountAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    //ignore
                    _logger.LogWarning("异常：{msg}", e);
                }
            }
            _logger.LogInformation("·开始推送·{task}·{user}", $"{this.Description()}任务", "");
        }

        protected abstract Task DoEachAccountAsync(CancellationToken cancellationToken);
    }
}
