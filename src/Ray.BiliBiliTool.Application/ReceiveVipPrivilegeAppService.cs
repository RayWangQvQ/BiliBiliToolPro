using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class ReceiveVipPrivilegeAppService : AppService, IReceiveVipPrivilegeAppService
    {
        private readonly ILogger<ReceiveVipPrivilegeAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAccountDomainService _accountDomainService;
        private readonly IVipPrivilegeDomainService _vipPrivilegeDomainService;
        private readonly IAccountDomainService _loginDomainService;
        private readonly Dictionary<string, int> _expDic;

        public ReceiveVipPrivilegeAppService(
            IConfiguration configuration,
            ILogger<ReceiveVipPrivilegeAppService> logger,
            IAccountDomainService accountDomainService,
            IVipPrivilegeDomainService vipPrivilegeDomainService,
            IAccountDomainService loginDomainService,
            IOptionsMonitor<Dictionary<string, int>> dicOptions
            )
        {
            _configuration = configuration;
            _logger = logger;
            _accountDomainService = accountDomainService;
            _vipPrivilegeDomainService = vipPrivilegeDomainService;
            _loginDomainService = loginDomainService;
            _expDic = dicOptions.Get(Constants.OptionsNames.ExpDictionaryName);

        }

        [TaskInterceptor("领取大会员福利")]
        public override void DoTask()
        {
            UserInfo userInfo = Login();

            ReceiveVipPrivilege(ref userInfo);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor("登录")]
        private UserInfo Login()
        {
            UserInfo userInfo = _loginDomainService.LoginByCookie();
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie");//终止流程

            _expDic.TryGetValue("每日登录", out int exp);
            _logger.LogInformation("登录成功，经验+{exp} √", exp);

            return userInfo;
        }

        /// <summary>
        /// 每月领取大会员福利
        /// </summary>
        [TaskInterceptor("领取", rethrowWhenException: false)]
        private void ReceiveVipPrivilege(ref UserInfo userInfo)
        {
            var suc = _vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo);

            //如果领取成功，需要刷新账户信息（比如B币余额）
            if (suc)
            {
                try
                {
                    userInfo = _loginDomainService.LoginByCookie();
                }
                catch (Exception ex)
                {
                    _logger.LogError("领取福利成功，但之后刷新用户信息时异常，信息：{msg}", ex.Message);
                }
            }
        }
    }
}
