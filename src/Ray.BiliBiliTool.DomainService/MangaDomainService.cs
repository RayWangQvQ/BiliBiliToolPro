using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 漫画
/// </summary>
public class MangaDomainService(
    ILogger<MangaDomainService> logger,
    IMangaApi mangaApi,
    IOptionsMonitor<MangaTaskOptions> mangaTaskOptions,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
    IOptionsMonitor<VipPrivilegeOptions> vipPrivilegeOptions
) : IMangaDomainService
{
    private readonly MangaTaskOptions _mangaTaskOptions = mangaTaskOptions.CurrentValue;
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly VipPrivilegeOptions _vipPrivilegeOptions = vipPrivilegeOptions.CurrentValue;

    /// <summary>
    /// 漫画签到
    /// </summary>
    public async Task MangaSign(BiliCookie ck)
    {
        BiliApiResponse response;
        try
        {
            response = await mangaApi.ClockIn(_dailyTaskOptions.DevicePlatform, ck.ToString());
        }
        catch (Exception)
        {
            //ignore
            //重复签到会报400异常,这里忽略掉
            logger.LogInformation("【签到结果】失败");
            logger.LogInformation("【原因】今日已签到过，无法重复签到");
            return;
        }

        if (response.Code == 0)
        {
            logger.LogInformation("【签到结果】成功");
        }
        else
        {
            logger.LogInformation("【签到结果】失败");
            logger.LogInformation("【原因】{msg}", response.Message);
        }
    }

    /// <summary>
    /// 漫画阅读
    /// </summary>
    public async Task MangaRead(BiliCookie ck)
    {
        if (_mangaTaskOptions.CustomComicId <= 0)
            return;
        BiliApiResponse response = await mangaApi.ReadManga(
            _dailyTaskOptions.DevicePlatform,
            _mangaTaskOptions.CustomComicId,
            _mangaTaskOptions.CustomEpId,
            ck.ToString()
        );

        if (response.Code == 0)
        {
            logger.LogInformation("【漫画阅读】成功");
        }
        else
        {
            logger.LogInformation("【漫画阅读】失败");
            logger.LogInformation("【原因】{msg}", response.Message);
        }
    }

    /// <summary>
    /// 获取大会员漫画权益
    /// </summary>
    /// <param name="reason_id">权益号，由https://api.bilibili.com/x/vip/privilege/my得到权益号数组，取值范围为数组中的整数
    /// 这里为方便直接取1，为领取漫读劵，暂时不取其他的值</param>
    public async Task ReceiveMangaVipReward(int reason_id, UserInfo userInfo, BiliCookie ck)
    {
        if (userInfo.GetVipType() == 0)
        {
            logger.LogInformation("不是会员，跳过");
            return;
        }

        int day = DateTime.Today.Day;
        logger.LogInformation("【今天】{day}号", day);

        var response = await mangaApi.ReceiveMangaVipReward(reason_id, ck.ToString());
        if (response.Code == 0)
        {
            logger.LogInformation("【领取结果】成功");
            logger.LogInformation($"【获取】{response.Data.Amount}张漫读劵");
        }
        else
        {
            logger.LogInformation("【领取结果】失败");
            logger.LogInformation("【原因】{msg}", response.Message);
        }
    }
}
