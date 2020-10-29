using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Attributes;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    public class VipPrivilegeDomainService : IVipPrivilegeDomainService
    {
        private readonly ILogger<VipPrivilegeDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliBiliCookiesOptions _biliBiliCookiesOptions;

        public VipPrivilegeDomainService(ILogger<VipPrivilegeDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            IOptionsMonitor<BiliBiliCookiesOptions> biliBiliCookiesOptions)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookiesOptions = biliBiliCookiesOptions.CurrentValue;
        }

        /// <summary>
        /// 每月1号领取大会员福利（B币券、大会员权益）
        /// </summary>
        [LogIntercepter("领取每月大会员权益")]

        public void ReceiveVipPrivilege(UseInfo useInfo)
        {
            int day = DateTime.Today.Day;

            //大会员类型
            int vipType = useInfo.GetVipType();

            if (day == 1 && vipType == 2)
            {
                ReceiveVipPrivilege(1);
                ReceiveVipPrivilege(2);
            }

            if (vipType == 0 || vipType == 1)
            {
                _logger.LogInformation("普通会员和月度大会员每月不赠送B币券，所以没法给自己充电哦");
            }
        }


        #region private
        /// <summary>
        /// 领取大会员每月赠送福利
        /// </summary>
        /// <param name="type">1.大会员B币券；2.大会员福利</param>
        private void ReceiveVipPrivilege(int type)
        {
            var response = _dailyTaskApi.ReceiveVipPrivilege(type, _biliBiliCookiesOptions.BiliJct).Result;
            if (response.Code == 0)
            {
                if (type == 1)
                {
                    _logger.LogInformation("领取年度大会员每月赠送的B币券成功");
                }
                else if (type == 2)
                {
                    _logger.LogInformation("领取大会员福利/权益成功");
                }
            }
            else
            {
                _logger.LogDebug($"领取年度大会员每月赠送的B币券/大会员福利失败，原因: {response.Message}");
            }
        }
        #endregion
    }
}
