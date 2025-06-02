using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask.ThreeDaysSign;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

public class VipBigPointDomainService(
    ILogger<VipBigPointDomainService> logger,
    IOptionsMonitor<VipBigPointOptions> vipBigPointOptions,
    IVipBigPointApi vipApi,
    IMallApi mallApi,
    IVipMallApi vipMallApi,
    IVideoApi videoApi,
    IAccountDomainService accountDomainService,
    IVideoDomainService videoDomainService
) : IVipBigPointDomainService
{
    private readonly VipBigPointOptions _vipBigPointOptions = vipBigPointOptions.CurrentValue;

    public async Task<VipBigPointCombine> GetCombineAsync(BiliCookie ck)
    {
        var allTasks = await mallApi.GetCombineAsync(
            new GetCombineRequest { csrf = ck.BiliJct, buvid = ck.Buvid },
            ck.ToString()
        );
        if (allTasks.Code != 0)
            throw new Exception(allTasks.ToJsonStr());
        return allTasks.Data;
    }

    /// <summary>
    /// 领取大会员专属等级加速包
    /// </summary>
    public async Task VipExpressAsync(BiliCookie ck)
    {
        var re = await vipApi.GetVouchersInfoAsync(ck.ToString());
        if (re.Code == 0)
        {
            var state = re.Data.List.Find(x => x.Type == 9)?.State;

            switch (state)
            {
                case 2:
                    logger.LogInformation("大会员经验观看任务未完成");
                    logger.LogInformation("开始观看视频");
                    // 观看视频，暂时没有好办法解决，先这样使着
                    DailyTaskInfo dailyTaskInfo = await accountDomainService.GetDailyTaskStatus(ck);
                    await videoDomainService.WatchAndShareVideo(dailyTaskInfo, ck);
                    // 跳转到未兑换，执行兑换任务
                    goto case 0;

                case 1:
                    logger.LogInformation("大会员经验已兑换");
                    break;

                case 0:
                    logger.LogInformation("大会员经验未兑换");
                    //兑换api
                    var response = await vipApi.ObtainVipExperienceAsync(
                        new VipExperienceRequest { csrf = ck.BiliJct },
                        ck.ToString()
                    );
                    if (response.Code != 0)
                    {
                        logger.LogInformation(
                            "大会员经验领取失败，错误信息：{message}",
                            response.Message
                        );
                        break;
                    }

                    logger.LogInformation("领取成功，经验+10 √");
                    var combine = await GetCombineAsync(ck);
                    combine.LogPointInfo(logger);
                    break;

                default:
                    logger.LogDebug("大会员经验领取失败，未知错误");
                    break;
            }
        }
    }

    /// <summary>
    /// 签到
    /// </summary>
    /// <param name="ck"></param>
    /// <exception cref="Exception"></exception>
    public async Task SignAsync(BiliCookie ck)
    {
        var signInfo = await vipApi.GetThreeDaySignAsync(
            new ThreeDaySignRequest { csrf = ck.BiliJct },
            ck.ToString()
        );
        if (signInfo.Data.three_day_sign.signed)
        {
            logger.LogInformation("已完成，跳过");
            logger.LogInformation(signInfo.Data.ToString());
            return;
        }

        BiliApiResponse<Sign2Response> re = await mallApi.Sign2Async(
            new Sign2RequestPath(ck.BiliJct),
            new Sign2Request(),
            ck.ToString()
        );
        if (re.Code != 0)
            throw new Exception(re.ToJsonStr());

        logger.LogInformation("签到成功");
        logger.LogInformation(re.Data.ToString());

        signInfo = await vipApi.GetThreeDaySignAsync(
            new ThreeDaySignRequest { csrf = ck.BiliJct },
            ck.ToString()
        );
        signInfo.Data.LogPointInfo(logger);
    }

    /// <summary>
    /// 领取任务
    /// </summary>
    /// <param name="combine"></param>
    /// <param name="ck"></param>
    public async Task ReceiveDailyMissionsAsync(VipBigPointCombine combine, BiliCookie ck)
    {
        const string moduleCode = "日常任务";

        var module = combine.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        var missionsNeedReceive = module?.common_task_item.Where(x => x.state == 0).ToList();
        if (missionsNeedReceive == null || missionsNeedReceive.Count == 0)
        {
            logger.LogInformation("均已领取，跳过");
            return;
        }

        foreach (var targetTask in missionsNeedReceive)
        {
            logger.LogInformation("开始领取任务：{task}", targetTask.title);
            await TryReceive(targetTask.task_code, ck);
        }
    }

    public async Task ReceiveAndCompleteAsync(
        VipBigPointCombine info,
        string moduleCode,
        string taskCode,
        BiliCookie ck,
        Func<string, BiliCookie, Task<bool>> completeFunc
    )
    {
        var module = info.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        var bonusTask = module?.common_task_item.FirstOrDefault(x => x.task_code == taskCode);

        if (bonusTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        if (bonusTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        if (bonusTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(bonusTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await completeFunc(taskCode, ck);

        //确认
        if (re)
        {
            var combine = await GetCombineAsync(ck);
            module = combine.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
            bonusTask = module?.common_task_item.FirstOrDefault(x => x.task_code == taskCode);
            var success = bonusTask is { state: 3, complete_times: >= 1 };
            logger.LogInformation("确认：{re}", success ? "成功，经验 +10" : "失败");
        }
    }

    public async Task<bool> CompleteAsync(string taskCode, BiliCookie ck)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await vipApi.CompleteAsync(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }

        logger.LogInformation("失败：{msg}", re.ToJsonStr());
        return false;
    }

    public async Task<bool> CompleteViewAsync(string taskCode, BiliCookie ck)
    {
        var channel = "jp_channel";

        logger.LogInformation("开始浏览");
        await Task.Delay(10 * 1000);

        var request = new ViewRequest(channel);
        var re = await vipApi.ViewComplete(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("浏览完成");
            return true;
        }

        logger.LogInformation("浏览失败：{msg}", re.ToJsonStr());
        return false;
    }

    public async Task<bool> CompleteViewVipMallAsync(string taskCode, BiliCookie ck)
    {
        var re = await vipMallApi.ViewVipMallAsync(
            new ViewVipMallRequest { Csrf = ck.BiliJct },
            ck.ToString()
        );
        if (re.Code != 0)
            throw new Exception(re.ToJsonStr());
        return true;
    }

    public async Task<bool> CompleteV2Async(string taskCode, BiliCookie ck)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await vipApi.CompleteV2(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }

        logger.LogInformation("失败：{msg}", re.ToJsonStr());
        return false;
    }

    #region private

    /// <summary>
    /// 领取任务
    /// </summary>
    private async Task TryReceive(string taskCode, BiliCookie ck)
    {
        BiliApiResponse? re = null;
        try
        {
            var request = new ReceiveOrCompleteTaskRequest(taskCode);
            re = await vipApi.ReceiveV2(request, ck.ToString());
            if (re.Code == 0)
                logger.LogInformation("领取任务成功");
            else
                logger.LogInformation("领取任务失败：{msg}", re.ToJsonStr());
        }
        catch (Exception e)
        {
            logger.LogError("领取任务异常");
            logger.LogError(e.Message + re?.ToJsonStr());
        }
    }

    private async Task<bool> WatchBangumi(BiliCookie ck)
    {
        if (_vipBigPointOptions.ViewBangumiList.Count == 0)
            return false;

        long randomSsid = _vipBigPointOptions.ViewBangumiList[
            new Random().Next(0, _vipBigPointOptions.ViewBangumiList.Count)
        ];

        var res = await GetBangumi(randomSsid, ck);
        if (res is null)
        {
            return false;
        }

        var videoInfo = res.Value.Item1;

        // 随机播放时间
        int playedTime = new Random().Next(905, 1800);
        // 观看该视频
        var request = new UploadVideoHeartbeatRequest()
        {
            Aid = long.Parse(videoInfo.Aid),
            Bvid = videoInfo.Bvid,
            Cid = videoInfo.Cid,
            Mid = long.Parse(ck.UserId),
            Sid = randomSsid,
            Epid = res.Value.Item2,
            Csrf = ck.BiliJct,
            Type = 4,
            Sub_type = 1,
            Start_ts = DateTime.Now.ToTimeStamp() - playedTime,
            Played_time = playedTime,
            Realtime = playedTime,
            Real_played_time = playedTime,
        };
        BiliApiResponse apiResponse = await videoApi.UploadVideoHeartbeat(request, ck.ToString());
        if (apiResponse.Code == 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 从自定义的番剧ssid中选择其中的一部中的一集
    /// </summary>
    /// <param name="randomSsid">番剧ssid</param>
    /// <returns></returns>
    private async Task<(VideoInfoDto, long)?> GetBangumi(long randomSsid, BiliCookie ck)
    {
        try
        {
            if (randomSsid is 0 or long.MinValue)
                return null;
            var bangumiInfo = await videoApi.GetBangumiBySsid(randomSsid, ck.ToString());

            // 从获取的剧集中随机获得其中的一集

            var bangumi = bangumiInfo.Result.episodes[
                new Random().Next(0, bangumiInfo.Result.episodes.Count)
            ];
            var videoInfo = new VideoInfoDto()
            {
                Bvid = bangumi.bvid,
                Aid = bangumi.aid.ToString(),
                Cid = bangumi.cid,
                Copyright = 1,
                Duration = bangumi.duration,
                Title = bangumi.share_copy,
            };
            logger.LogInformation("本次播放的正片为：{title}", bangumi.share_copy);
            return (videoInfo, bangumi.ep_id);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }

        return null;
    }

    #endregion
}
