using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Extensions;

namespace Ray.BiliBiliTool.DomainService
{
    public class ChargeDomainService : IChargeDomainService
    {
        private readonly ILogger<ChargeDomainService> _logger;
        private readonly IOptionsMonitor<DailyTaskOptions> _dailyTaskOptions;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliBiliCookiesOptions _verify;

        public ChargeDomainService(ILogger<ChargeDomainService> logger,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IDailyTaskApi dailyTaskApi,
            BiliBiliCookiesOptions verify)
        {
            _logger = logger;
            _dailyTaskOptions = dailyTaskOptions;
            _dailyTaskApi = dailyTaskApi;
            _verify = verify;
        }

        /// <summary>
        /// 月底自动给自己充电
        /// 仅充会到期的B币券，低于2的时候不会充
        /// </summary>
        public void Charge(UseInfo userInfo)
        {
            if (!_dailyTaskOptions.CurrentValue.MonthEndAutoCharge) return;

            int lastDay = DateTime.Today.LastDayOfMonth().Day;
            if (DateTime.Today.Day != lastDay)
            {
                _logger.LogInformation($"今天是本月的第: {DateTime.Today.Day}天，等到{lastDay}号会自动为您充电哒");
                //desp.appendDesp("今天是本月的第: " + day + "天，还没到给自己充电日子呢");
                return;
            }

            //B币券余额
            int couponBalance = userInfo.Wallet.Coupon_balance;
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

            var response = _dailyTaskApi.Charge(couponBalance * 10, _verify.UserId, _verify.UserId, _verify.BiliJct).Result;
            if (response.Code == 0)
            {
                if (response.Data.Status == 4)
                {
                    _logger.LogInformation("月底了，给自己充电成功啦，送的B币券没有浪费哦");
                    _logger.LogInformation("本次给自己充值了: " + couponBalance * 10 + "个电池哦");
                    //desp.appendDesp("本次给自己充值了: " + couponBalance * 10 + "个电池哦");

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
            _dailyTaskApi.ChargeComment(token, "Ray.BiliBiliTool自动充电", _verify.BiliJct);
        }
    }
}
