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
        /// 月底自动己充电
        /// 仅充会到期的B币券，低于2的时候不会充
        /// </summary>
        public void Charge(UserInfo userInfo)
        {
            if (_dailyTaskOptions.DayOfAutoCharge == 0)
            {
                _logger.LogInformation("已配置为关闭，跳过");
                return;
            }

            //大会员类型
            int vipType = userInfo.GetVipType();
            if (vipType != 2)
            {
                _logger.LogInformation("不是年度大会员，跳过");
                return;
            }

            int targetDay = _dailyTaskOptions.DayOfAutoCharge == -1
                ? DateTime.Today.LastDayOfMonth().Day
                : _dailyTaskOptions.DayOfAutoCharge;

            _logger.LogInformation("【目标日期】{targetDay}号", targetDay);
            _logger.LogInformation("【今天】{today}号", DateTime.Today.Day);

            if (DateTime.Today.Day != targetDay)
            {
                _logger.LogInformation("跳过");
                return;
            }

            //B币券余额
            decimal couponBalance = userInfo.Wallet.Coupon_balance;
            _logger.LogInformation("【B币券】{couponBalance}", couponBalance);
            if (couponBalance < 2)
            {
                _logger.LogInformation("余额小于2，无法充电");
                return;
            }

            string targetUpId = _dailyTaskOptions.AutoChargeUpId;
            //如果没有配置或配了-1，则为自己充电
            if (_dailyTaskOptions.AutoChargeUpId.IsNullOrEmpty() | _dailyTaskOptions.AutoChargeUpId == "-1")
                targetUpId = _cookie.UserId;

            _logger.LogDebug("【目标Up】{up}", targetUpId);

            var request = new ChargeRequest(couponBalance, long.Parse(targetUpId), _cookie.BiliJct);

            //BiliApiResponse<ChargeResponse> response = _chargeApi.Charge(decimal.ToInt32(couponBalance * 10), _dailyTaskOptions.AutoChargeUpId, _cookieOptions.UserId, _cookieOptions.BiliJct).Result;
            BiliApiResponse<ChargeV2Response> response = _chargeApi.ChargeV2(request)
                .GetAwaiter().GetResult();

            if (response.Code == 0)
            {
                if (response.Data.Status == 4)
                {
                    _logger.LogInformation("【充电结果】成功");
                    _logger.LogInformation("【充值个数】 {num}个B币", couponBalance);
                    _logger.LogInformation("经验+{exp} √", couponBalance);
                    _logger.LogInformation("在过期前使用成功，赠送的B币券没有浪费哦~");

                    //充电留言
                    ChargeComments(response.Data.Order_no);
                }
                else
                {
                    _logger.LogInformation("【充电结果】失败");
                    _logger.LogError("【原因】{reason}", response.ToJson());
                }
            }
            else
            {
                _logger.LogInformation("【充电结果】失败");
                _logger.LogError("【原因】{reason}", response.Message);
            }
        }

        /// <summary>
        /// 充电后留言
        /// </summary>
        /// <param name="token"></param>
        public void ChargeComments(string orderNum)
        {
            var comment = _dailyTaskOptions.ChargeComment ?? "";
            var request = new ChargeCommentRequest(orderNum, comment, _cookie.BiliJct);
            _chargeApi.ChargeComment(request).GetAwaiter().GetResult();

            _logger.LogInformation("【留言】{comment}", comment);
        }
    }
}
