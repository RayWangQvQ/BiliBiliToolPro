using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 会员权益
    /// </summary>
    public class VipPrivilegeDomainService : IVipPrivilegeDomainService
    {
        private readonly ILogger<VipPrivilegeDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly BiliBiliCookieOptions _biliBiliCookieOptions;

        public VipPrivilegeDomainService(ILogger<VipPrivilegeDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            IOptionsMonitor<BiliBiliCookieOptions> biliBiliCookieOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _biliBiliCookieOptions = biliBiliCookieOptions.CurrentValue;
        }

        /// <summary>
        /// 每月1号领取大会员福利（B币券、大会员权益）
        /// </summary>
        /// <param name="useInfo"></param>
        public void ReceiveVipPrivilege(UseInfo useInfo)
        {
            if (_dailyTaskOptions.DayOfReceiveVipPrivilege == 0)
            {
                _logger.LogInformation("已配置为不进行自动领取会员权益，跳过领取任务");
                return;
            }

            int targetDay = _dailyTaskOptions.DayOfReceiveVipPrivilege == -1
                ? 1
                : _dailyTaskOptions.DayOfReceiveVipPrivilege;

            if (DateTime.Today.Day != targetDay)
            {
                _logger.LogInformation("目标领取日期为{targetDay}号，今天是{day}号，跳过领取任务", targetDay, DateTime.Today.Day);
                return;
            }

            //大会员类型
            int vipType = useInfo.GetVipType();

            if (vipType == 2)
            {
                ReceiveVipPrivilege(1);
                ReceiveVipPrivilege(2);
            }
            else
            {
                _logger.LogInformation("普通会员和月度大会员每月不赠送B币券，所以不需要领取权益喽");
            }
        }


        #region private
        /// <summary>
        /// 领取大会员每月赠送福利
        /// </summary>
        /// <param name="type">1.大会员B币券；2.大会员福利</param>
        private void ReceiveVipPrivilege(int type)
        {
            var response = _dailyTaskApi.ReceiveVipPrivilege(type, _biliBiliCookieOptions.BiliJct).Result;
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
                _logger.LogInformation($"领取年度大会员每月赠送的B币券/大会员福利失败，原因: {response.Message}");
            }
        }
        #endregion
    }
}
