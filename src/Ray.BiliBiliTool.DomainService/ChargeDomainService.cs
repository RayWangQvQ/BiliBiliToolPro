using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Extensions;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 充电
    /// </summary>
    public class ChargeDomainService : IChargeDomainService
    {
        private readonly ILogger<ChargeDomainService> _logger;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliBiliCookieOptions _cookieOptions;

        public ChargeDomainService(ILogger<ChargeDomainService> logger,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IDailyTaskApi dailyTaskApi,
            IOptionsMonitor<BiliBiliCookieOptions> cookieOptions)
        {
            _logger = logger;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _dailyTaskApi = dailyTaskApi;
            _cookieOptions = cookieOptions.CurrentValue;
        }

        /// <summary>
        /// 月底自动给自己充电
        /// 仅充会到期的B币券，低于2的时候不会充
        /// </summary>
        public void Charge(UseInfo userInfo)
        {
            if (_dailyTaskOptions.DayOfAutoCharge == 0)
            {
                _logger.LogInformation("已配置为不进行自动充电，跳过充电任务");
                return;
            }

            int targetDay = _dailyTaskOptions.DayOfAutoCharge == -1
                ? DateTime.Today.LastDayOfMonth().Day
                : _dailyTaskOptions.DayOfAutoCharge;

            if (DateTime.Today.Day != targetDay)
            {
                _logger.LogInformation($"目标充电日期为{targetDay}号，今天是{DateTime.Today.Day}号，跳过充电任务");
                return;
            }

            //B币券余额
            var couponBalance = userInfo.Wallet.Coupon_balance;
            if (couponBalance < 2)
            {
                _logger.LogInformation("B币券余额<2,无法充电");
                return;
            }

            //大会员类型
            int vipType = userInfo.GetVipType();
            if (vipType != 2)
            {
                _logger.LogInformation("不是年度大会员或已过期,无法充电");
                return;
            }

            var response = _dailyTaskApi.Charge(couponBalance * 10, _cookieOptions.UserId, _cookieOptions.UserId, _cookieOptions.BiliJct).Result;
            if (response.Code == 0)
            {
                if (response.Data.Status == 4)
                {
                    _logger.LogInformation("给自己充电成功啦，送的B币券没有浪费哦");
                    _logger.LogInformation($"本次给自己充值了: {couponBalance * 10}个电池哦");

                    //获取充电留言token
                    ChargeComments(response.Data.Order_no);
                }
                else
                {
                    _logger.LogDebug("充电失败了啊 原因: " + JsonSerializer.Serialize(response));
                }
            }
            else
            {
                _logger.LogDebug("充电失败了啊 原因: " + response.Message);
            }
        }

        /// <summary>
        /// 充电后留言
        /// </summary>
        /// <param name="token"></param>
        public void ChargeComments(string token)
        {
            _dailyTaskApi.ChargeComment(token, "Ray.BiliBiliTool自动充电", _cookieOptions.BiliJct);
        }
    }
}
