using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 充电
/// </summary>
public class ChargeDomainService(
    ILogger<ChargeDomainService> logger,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
    IDailyTaskApi dailyTaskApi,
    CookieStrFactory<BiliCookie> cookieFactory,
    IChargeApi chargeApi
) : IChargeDomainService
{
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly IDailyTaskApi _dailyTaskApi = dailyTaskApi;

    /// <summary>
    /// 月底自动己充电
    /// 仅充会到期的B币券，低于2的时候不会充
    /// </summary>
    public async Task Charge(UserInfo userInfo, BiliCookie ck)
    {
        if (_dailyTaskOptions.DayOfAutoCharge == 0)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return;
        }

        //大会员类型
        VipType vipType = userInfo.GetVipType();
        if (vipType != VipType.Annual)
        {
            logger.LogInformation("不是年度大会员，跳过");
            return;
        }

        int targetDay =
            _dailyTaskOptions.DayOfAutoCharge == -1
                ? DateTime.Today.LastDayOfMonth().Day
                : _dailyTaskOptions.DayOfAutoCharge;

        logger.LogInformation("【目标日期】{targetDay}号", targetDay);
        logger.LogInformation("【今天】{today}号", DateTime.Today.Day);

        if (DateTime.Today.Day != targetDay)
        {
            logger.LogInformation("跳过");
            return;
        }

        //B币券余额
        decimal couponBalance = userInfo.Wallet.Coupon_balance;
        logger.LogInformation("【B币券】{couponBalance}", couponBalance);
        if (couponBalance < 2)
        {
            logger.LogInformation("余额小于2，无法充电");
            return;
        }

        string targetUpId = _dailyTaskOptions.AutoChargeUpId;
        //如果没有配置或配了-1，则为自己充电
        if (
            _dailyTaskOptions.AutoChargeUpId.IsNullOrEmpty()
            | _dailyTaskOptions.AutoChargeUpId == "-1"
        )
        {
            targetUpId = ck.UserId;
        }

        logger.LogDebug("【目标Up】{up}", targetUpId);

        var request = new ChargeRequest(couponBalance, long.Parse(targetUpId), ck.BiliJct);

        //BiliApiResponse<ChargeResponse> response = await _chargeApi.Charge(decimal.ToInt32(couponBalance * 10), _dailyTaskOptions.AutoChargeUpId, _cookieOptions.UserId, _cookieOptions.BiliJct);
        BiliApiResponse<ChargeV2Response> response = await chargeApi.ChargeV2Async(
            request,
            ck.ToString()
        );

        if (response.Code == 0)
        {
            if (response.Data.Status == 4)
            {
                logger.LogInformation("【充电结果】成功");
                logger.LogInformation("【充值个数】 {num}个B币", couponBalance);
                logger.LogInformation("经验+{exp} √", couponBalance);
                logger.LogInformation("在过期前使用成功，赠送的B币券没有浪费哦~");

                //充电留言
                await ChargeComments(response.Data.Order_no, ck);
            }
            else
            {
                logger.LogInformation("【充电结果】失败");
                logger.LogError("【原因】{reason}", response.ToJsonStr());
            }
        }
        else
        {
            logger.LogInformation("【充电结果】失败");
            logger.LogError("【原因】{reason}", response.Message);
        }
    }

    /// <summary>
    /// 充电后留言
    /// </summary>
    /// <param name="token"></param>
    public async Task ChargeComments(string orderNum, BiliCookie ck)
    {
        var comment = _dailyTaskOptions.ChargeComment ?? "";
        var request = new ChargeCommentRequest(orderNum, comment, ck.BiliJct);
        await chargeApi.ChargeCommentAsync(request, ck.ToString());

        logger.LogInformation("【留言】{comment}", comment);
    }
}
