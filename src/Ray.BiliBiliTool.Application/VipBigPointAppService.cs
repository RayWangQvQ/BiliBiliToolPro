using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ViewMall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class VipBigPointAppService(
    ILogger<VipBigPointAppService> logger,
    IVipBigPointApi vipApi,
    IAccountDomainService loginDomainService,
    IVideoDomainService videoDomainService,
    IAccountDomainService accountDomainService,
    IVipMallApi vipMallApi,
    IVideoApi videoApi,
    IOptionsMonitor<VipBigPointOptions> vipBigPointOptions,
    CookieStrFactory<BiliCookie> cookieFactory
) : BaseMultiAccountsAppService(logger, cookieFactory), IVipBigPointAppService
{
    private readonly VipBigPointOptions _vipBigPointOptions = vipBigPointOptions.CurrentValue;
    private VipTaskInfo _info;

    [TaskInterceptor("大会员大积分", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        var userInfo = await GetUserInfo(ck);
        if (userInfo.GetVipType() == VipType.None)
        {
            logger.LogInformation("当前不是大会员，跳过任务");
            return;
        }

        var allTasks = await vipApi.GetTaskListAsync(ck.ToString());
        if (allTasks.Code != 0)
            throw new Exception(allTasks.ToJsonStr());
        _info = allTasks.Data;
        _info.LogInfo(logger);

        await VipExpressAsync(ck);

        //签到
        await Sign(ck);

        //领取
        await ReceiveTasksAsync(ck);

        //福利任务
        await Bonus(ck);

        //体验任务
        await Privilege(ck);

        //日常任务
        //浏览追番频道页10秒
        await ViewAnimate(ck);

        //浏览会员购页面10秒
        await ViewVipMall(ck);

        //浏览装扮商城
        await ViewDressMall(ck);

        //观看剧集内容
        await OgvWatchAsync(ck);

        _info.LogInfo(logger);
    }

    [TaskInterceptor("测试Cookie")]
    private async Task<UserInfo> GetUserInfo(BiliCookie ck)
    {
        UserInfo userInfo = await loginDomainService.LoginByCookie(ck);
        if (userInfo == null)
            throw new Exception("登录失败，请检查Cookie"); //终止流程

        return userInfo;
    }

    /// <summary>
    /// 领取大会员专属经验包
    /// </summary>
    [TaskInterceptor("大会员经验领取任务", TaskLevel.Two, false)]
    private async Task VipExpressAsync(BiliCookie ck)
    {
        var re = await vipApi.GetVouchersInfoAsync(ck.ToString());
        if (re.Code == 0)
        {
            var state = re.Data.List.Find(x => x.Type == 9).State;

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
                        new VipExperienceRequest() { csrf = ck.BiliJct },
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
                    break;

                default:
                    logger.LogDebug("大会员经验领取失败，未知错误");
                    break;
            }
        }
    }

    [TaskInterceptor("签到", TaskLevel.Two, false)]
    private async Task Sign(BiliCookie ck)
    {
        if (_info.Task_info.Sing_task_item.IsTodaySigned)
        {
            logger.LogInformation("已完成，跳过");
            logger.LogInformation(
                "今日获得签到积分：{score}",
                _info.Task_info.Sing_task_item.TodayHistory?.Score
            );
            logger.LogInformation("累计签到{count}天", _info.Task_info.Sing_task_item.Count);
            return;
        }

        var re = await vipApi.SignAsync(new SignRequest(), ck.ToString());
        if (re.Code != 0)
            throw new Exception(re.ToJsonStr());

        //确认
        var infoResult = await vipApi.GetTaskListAsync(ck.ToString());
        if (infoResult.Code != 0)
            throw new Exception(infoResult.ToJsonStr());
        _info = infoResult.Data;

        logger.LogInformation(
            "今日可获得签到积分：{score}",
            _info.Task_info.Sing_task_item.TodayHistory?.Score
        );
        logger.LogInformation(
            _info.Task_info.Sing_task_item.IsTodaySigned ? "签到成功" : "签到失败"
        );
        logger.LogInformation("累计签到{count}天", _info.Task_info.Sing_task_item.Count);
    }

    [TaskInterceptor("领取任务", TaskLevel.Two, false)]
    private async Task ReceiveTasksAsync(BiliCookie ck)
    {
        const string moduleCode = "日常任务";

        var module = _info.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        var needReceiveTasks = module?.common_task_item.Where(x => x.state == 0).ToList();
        if (needReceiveTasks == null || !needReceiveTasks.Any())
        {
            logger.LogInformation("均已领取，跳过");
            return;
        }

        foreach (var targetTask in needReceiveTasks)
        {
            logger.LogInformation("开始领取任务：{task}", targetTask.title);
            await TryReceive(targetTask.task_code, ck);
        }
    }

    [TaskInterceptor("福利任务", TaskLevel.Two, false)]
    private async Task Bonus(BiliCookie ck)
    {
        const string moduleCode = "福利任务";
        const string taskCode = "bonus";

        var bonusTask = GetTarget(_info, moduleCode, taskCode);

        if (bonusTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        //如果状态不等于3，则做
        if (bonusTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        //0需要领取
        if (bonusTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(bonusTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await Complete(bonusTask.task_code, ck);

        //确认
        if (re)
        {
            var infoResult = await vipApi.GetTaskListAsync(ck.ToString());
            if (infoResult.Code != 0)
                throw new Exception(infoResult.ToJsonStr());
            _info = infoResult.Data;
            bonusTask = GetTarget(_info, moduleCode, taskCode);

            logger.LogInformation(
                "确认：{re}",
                bonusTask.state == 3 && bonusTask.complete_times >= 1
            );
        }
    }

    [TaskInterceptor("体验任务", TaskLevel.Two, false)]
    private async Task Privilege(BiliCookie ck)
    {
        const string moduleCode = "体验任务";
        const string taskCode = "privilege";

        var privilegeTask = GetTarget(_info, moduleCode, taskCode);

        if (privilegeTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        //如果状态不等于3，则做
        if (privilegeTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        //0需要领取
        if (privilegeTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(privilegeTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await Complete(privilegeTask.task_code, ck);

        //确认
        if (re)
        {
            var infoResult = await vipApi.GetTaskListAsync(ck.ToString());
            if (infoResult.Code != 0)
                throw new Exception(infoResult.ToJsonStr());
            _info = infoResult.Data;
            privilegeTask = GetTarget(_info, moduleCode, taskCode);

            logger.LogInformation(
                "确认：{re}",
                privilegeTask.state == 3 && privilegeTask.complete_times >= 1
            );
        }
    }

    [TaskInterceptor("浏览追番频道页10秒", TaskLevel.Two, false)]
    private async Task ViewAnimate(BiliCookie ck)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "animatetab";

        var code = "jp_channel";

        CommonTaskItem targetTask = GetTarget(_info, moduleCode, taskCode);

        if (targetTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await CompleteView(code, ck);

        //确认
        if (re)
        {
            var infoResult = await vipApi.GetTaskListAsync(ck.ToString());
            if (infoResult.Code != 0)
                throw new Exception(infoResult.ToJsonStr());
            _info = infoResult.Data;
            targetTask = GetTarget(_info, moduleCode, taskCode);

            logger.LogInformation(
                "确认：{re}",
                targetTask.state == 3 && targetTask.complete_times >= 1
            );
        }
    }

    [TaskInterceptor("浏览会员购页面10秒", TaskLevel.Two, false)]
    private async Task ViewVipMall(BiliCookie ck)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "vipmallview";

        CommonTaskItem targetTask = GetTarget(_info, moduleCode, taskCode);

        if (targetTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await vipMallApi.ViewVipMallAsync(
            new ViewVipMallRequest() { Csrf = ck.BiliJct },
            ck.ToString()
        );
        if (re.Code != 0)
            throw new Exception(re.ToJsonStr());

        //确认
        if (re.Code == 0)
        {
            var infoResult = await vipApi.GetTaskListAsync(ck.ToString());
            if (infoResult.Code != 0)
                throw new Exception(infoResult.ToJsonStr());
            _info = infoResult.Data;
            targetTask = GetTarget(_info, moduleCode, taskCode);

            logger.LogInformation(
                "确认：{re}",
                targetTask.state == 3 && targetTask.complete_times >= 1
            );
        }
    }

    [TaskInterceptor("浏览装扮商城主页", TaskLevel.Two, false)]
    private async Task ViewDressMall(BiliCookie ck)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "dress-view";

        CommonTaskItem targetTask = GetTarget(_info, moduleCode, taskCode);

        if (targetTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await CompleteV2(targetTask.task_code, ck);

        //确认
        if (re)
        {
            var infoResult = await vipApi.GetTaskListAsync(ck.ToString());
            if (infoResult.Code != 0)
                throw new Exception(infoResult.ToJsonStr());
            _info = infoResult.Data;
            targetTask = GetTarget(_info, moduleCode, taskCode);

            logger.LogInformation(
                "确认：{re}",
                targetTask.state == 3 && targetTask.complete_times >= 1
            );
        }
    }

    [TaskInterceptor("观看剧集", TaskLevel.Two, false)]
    private async Task OgvWatchAsync(BiliCookie ck)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "ogvwatchnew";

        CommonTaskItem targetTask = GetTarget(_info, moduleCode, taskCode);

        if (targetTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code, ck);
        }

        logger.LogInformation("暂未实现");
    }

    #region private

    private static CommonTaskItem GetTarget(VipTaskInfo info, string moduleCode, string taskCode)
    {
        var module = info.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        return module?.common_task_item.FirstOrDefault(x => x.task_code == taskCode);
    }

    /// <summary>
    /// 领取任务
    /// </summary>
    private async Task TryReceive(string taskCode, BiliCookie ck)
    {
        BiliApiResponse re = null;
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

    private async Task<bool> Complete(string taskCode, BiliCookie ck)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await vipApi.CompleteAsync(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }
        else
        {
            logger.LogInformation("失败：{msg}", re.ToJsonStr());
            return false;
        }
    }

    private async Task<bool> CompleteV2(string taskCode, BiliCookie ck)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await vipApi.CompleteV2(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }
        else
        {
            logger.LogInformation("失败：{msg}", re.ToJsonStr());
            return false;
        }
    }

    private async Task<bool> CompleteView(string code, BiliCookie ck)
    {
        logger.LogInformation("开始浏览");
        await Task.Delay(10 * 1000);

        var request = new ViewRequest(code);
        var re = await vipApi.ViewComplete(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("浏览完成");
            return true;
        }
        else
        {
            logger.LogInformation("浏览失败：{msg}", re.ToJsonStr());
            return false;
        }
    }

    private async Task<bool> WatchBangumi(BiliCookie ck)
    {
        if (
            _vipBigPointOptions.ViewBangumiList == null
            || _vipBigPointOptions.ViewBangumiList.Count == 0
        )
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
