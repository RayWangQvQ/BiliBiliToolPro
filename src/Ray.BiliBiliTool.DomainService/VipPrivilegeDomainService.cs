using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
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

        public VipPrivilegeDomainService(
            ILogger<VipPrivilegeDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            IOptionsMonitor<BiliBiliCookieOptions> biliBiliCookieOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions
            )
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _biliBiliCookieOptions = biliBiliCookieOptions.CurrentValue;
        }

        /// <summary>
        /// 每月领取大会员福利（B币券、大会员权益）
        /// </summary>
        /// <param name="useInfo"></param>
        public bool ReceiveVipPrivilege(UserInfo userInfo)
        {
            if (_dailyTaskOptions.DayOfReceiveVipPrivilege == 0)
            {
                _logger.LogInformation("已配置为不进行自动领取会员权益，跳过领取任务");
                return false;
            }

            int targetDay = _dailyTaskOptions.DayOfReceiveVipPrivilege == -1
                ? 1
                : _dailyTaskOptions.DayOfReceiveVipPrivilege;

            if (DateTime.Today.Day != targetDay
                && DateTime.Today.Day != DateTime.Today.LastDayOfMonth().Day)
            {
                _logger.LogInformation("目标领取日期为{targetDay}号，今天是{day}号，跳过领取任务", targetDay, DateTime.Today.Day);
                return false;
            }

            //大会员类型
            int vipType = userInfo.GetVipType();

            if (vipType == 2)
            {
                var suc1 = ReceiveVipPrivilege(1);
                var suc2 = ReceiveVipPrivilege(2);

                if (suc1 | suc2) return true;
                return false;
            }
            else
            {
                _logger.LogInformation("普通会员和月度大会员每月不赠送B币券，所以不需要领取权益喽");
                return false;
            }
        }

        #region private

        /// <summary>
        /// 领取大会员每月赠送福利
        /// </summary>
        /// <param name="type">1.大会员B币券；2.大会员福利</param>
        private bool ReceiveVipPrivilege(int type)
        {
            var response = _dailyTaskApi.ReceiveVipPrivilege(type, _biliBiliCookieOptions.BiliJct).Result;

            var name = GetPrivilegeName(type);
            if (response.Code == 0)
            {
                _logger.LogInformation($"{name}成功");
                return true;
            }
            else
            {
                _logger.LogInformation($"{name}失败，原因: {response.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取权益名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetPrivilegeName(int type)
        {
            switch (type)
            {
                case 1:
                    return "领取年度大会员每月赠送的B币券";

                case 2:
                    return "领取大会员福利/权益";
            }

            return "";
        }

        #endregion private
    }
}
