using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

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
        private readonly BiliCookie _cookie;
        private readonly IChargeApi _chargeApi;

        public ChargeDomainService(
            ILogger<ChargeDomainService> logger,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IDailyTaskApi dailyTaskApi,
            BiliCookie cookie,
            IChargeApi chargeApi
            )
        {
            _logger = logger;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _dailyTaskApi = dailyTaskApi;
            _cookie = cookie;
            _chargeApi = chargeApi;
        }

        /// <summary>
        /// 月底自动给自己充电
        /// 仅充会到期的B币券，低于2的时候不会充
        /// </summary>
        public void Charge(UserInfo userInfo)
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
                _logger.LogInformation("目标充电日期为{targetDay}号，今天是{today}号，跳过充电任务", targetDay, DateTime.Today.Day);
                return;
            }

            //B币券余额
            decimal couponBalance = userInfo.Wallet.Coupon_balance;
            if (couponBalance < 2)
            {
                _logger.LogInformation("B币小于2，无法充电");
                return;
            }

            //大会员类型
            int vipType = userInfo.GetVipType();
            if (vipType != 2)
            {
                _logger.LogInformation("不是年度大会员或已过期，不进行B币券自动充电");
                return;
            }

            string targetUpId = _dailyTaskOptions.AutoChargeUpId;
            //如果没有配置或配了-1，则为自己充电
            if (_dailyTaskOptions.AutoChargeUpId.IsNullOrEmpty() | _dailyTaskOptions.AutoChargeUpId == "-1")
                targetUpId = _cookie.UserId;

            var request = new ChargeRequest(couponBalance, long.Parse(targetUpId), _cookie.BiliJct);

            //BiliApiResponse<ChargeResponse> response = _chargeApi.Charge(decimal.ToInt32(couponBalance * 10), _dailyTaskOptions.AutoChargeUpId, _cookieOptions.UserId, _cookieOptions.BiliJct).Result;
            BiliApiResponse<ChargeV2Response> response = _chargeApi.ChargeV2(request)
                .GetAwaiter().GetResult();

            if (response.Code == 0)
            {
                if (response.Data.Status == 4)
                {
                    _logger.LogInformation("充电成功，经验+{exp} √", couponBalance);
                    _logger.LogInformation("本次为{upId}充值了: {num}个B币，送的B币券没有浪费哦", targetUpId, couponBalance);

                    //获取充电留言token
                    ChargeComments(response.Data.Order_no);
                }
                else
                {
                    _logger.LogError("充电失败了啊 原因：{reason}", response.ToJson());
                }
            }
            else
            {
                _logger.LogError("充电失败了啊 原因：{reason}", response.Message);
            }
        }

        /// <summary>
        /// 充电后留言
        /// </summary>
        /// <param name="token"></param>
        public void ChargeComments(string orderNum)
        {
            var request = new ChargeCommentRequest(orderNum, _dailyTaskOptions.ChargeComment ?? "", _cookie.BiliJct);
            _chargeApi.ChargeComment(request);
        }
    }
}
