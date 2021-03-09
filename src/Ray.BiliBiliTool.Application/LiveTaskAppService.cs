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
    public class LiveTaskAppService : ILiveTaskAppService
    {
        private readonly ILogger<LiveTaskAppService> _logger;
        private readonly ILiveDomainService _liveDomainService;
        private readonly SecurityOptions _securityOptions;
        private readonly IAccountDomainService _accountDomainService;

        public LiveTaskAppService(
            ILiveDomainService liveDomainService,
            IOptionsMonitor<SecurityOptions> securityOptions,
            ILogger<LiveTaskAppService> logger,
            IAccountDomainService accountDomainService)
        {
            _liveDomainService = liveDomainService;
            _securityOptions = securityOptions.CurrentValue;
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        public void DoLotteryTask()
        {
            if (_securityOptions.IsSkipDailyTask)
            {
                _logger.LogWarning("已配置为跳过任务\r\n");
                return;
            }

            if (_securityOptions.RandomSleepMaxMin > 0)
            {
                int randomMin = new Random().Next(1, ++_securityOptions.RandomSleepMaxMin);
                _logger.LogInformation("随机休眠{min}分钟 \r\n", randomMin);
                Thread.Sleep(randomMin * 1000 * 60);
            }

            UserInfo userInfo = Login();

            LotteryTianXuan();
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor("登录")]
        private UserInfo Login()
        {
            UserInfo userInfo = _accountDomainService.LoginByCookie();
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie");//终止流程

            return userInfo;
        }

        [TaskInterceptor("天选时刻抽奖")]
        private void LotteryTianXuan()
        {
            _liveDomainService.TianXuan();
        }
    }
}
