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

namespace Ray.BiliBiliTool.Application;

public class VipBigPointAppService : AppService, IVipBigPointAppService
{
    private readonly ILogger<VipBigPointAppService> _logger;
    private readonly IVipBigPointApi _vipApi;
    private readonly IAccountDomainService _loginDomainService;
    private readonly IVideoDomainService _videoDomainService;
    private readonly IAccountDomainService _accountDomainService;
    private readonly BiliCookie _biliCookie;
    private readonly IVipMallApi _vipMallApi;
    private readonly IVideoApi _videoApi;
    private readonly VipBigPointOptions _vipBigPointOptions;

    public VipBigPointAppService(
        ILogger<VipBigPointAppService> logger,
        IVipBigPointApi vipApi,
        IAccountDomainService loginDomainService,
        IVideoDomainService videoDomainService,
        BiliCookie biliCookie,
        IAccountDomainService accountDomainService,
        IVipMallApi vipMallApi,
        IVideoApi videoApi,
        IOptionsMonitor<VipBigPointOptions> vipBigPointOptions)
    {
        _logger = logger;
        _vipApi = vipApi;
        _loginDomainService = loginDomainService;
        _videoDomainService = videoDomainService;
        _biliCookie = biliCookie;
        _accountDomainService = accountDomainService;
        _vipMallApi = vipMallApi;
        _videoApi = videoApi;
        _vipBigPointOptions = vipBigPointOptions.CurrentValue;
    }

    [TaskInterceptor("大会员大积分", TaskLevel.One)]
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        // TODO 解决taskInfo在一个任务出错后，后续的任务均会报空引用错误
        var ui = await GetUserInfo();

        if (ui.GetVipType() == VipType.None)
        {
            _logger.LogInformation("当前不是大会员或已过期，跳过任务");
            return;
        }

        var re = await _vipApi.GetTaskListAsync();

        if (re.Code != 0) throw new Exception(re.ToJsonStr());

        VipTaskInfo taskInfo = re.Data;
        taskInfo.LogInfo(_logger);

        await VipExpress();

        //签到
        taskInfo = await Sign(taskInfo);

        //福利任务
        taskInfo = await Bonus(taskInfo);

        //体验任务
        taskInfo = await Privilege(taskInfo);

        //日常任务

        //浏览追番频道页10秒
        taskInfo = await ViewAnimate(taskInfo);

        //浏览会员购页面10秒
        taskInfo = await ViewVipMall(taskInfo);

        //浏览装扮商城
        taskInfo = await ViewDressMall(taskInfo);

        //观看剧集内容
        taskInfo = await ViewVideo(taskInfo);

        //领取购买任务
        taskInfo = await BuyVipVideo(taskInfo);
        taskInfo = await BuyVipMall(taskInfo);

        taskInfo.LogInfo(_logger);
    }

    /// <summary>
    /// 领取大会员专属经验包
    /// </summary>
    public async Task VipExpress()
    {
        _logger.LogInformation("大会员经验领取任务开始");
        var re = await _vipApi.GetVouchersInfoAsync();
        if (re.Code == 0)
        {
            var state = re.Data.List.Find(x => x.Type == 9).State;

            switch (state)
            {
                case 2:
                    _logger.LogInformation("大会员经验观看任务未完成");
                    _logger.LogInformation("开始观看视频");
                    // 观看视频，暂时没有好办法解决，先这样使着
                    DailyTaskInfo dailyTaskInfo = await _accountDomainService.GetDailyTaskStatus();
                    await _videoDomainService.WatchAndShareVideo(dailyTaskInfo);
                    // 跳转到未兑换，执行兑换任务
                    goto case 0;

                case 1:
                    _logger.LogInformation("大会员经验已兑换");
                    break;

                case 0:
                    _logger.LogInformation("大会员经验未兑换");
                    //兑换api
                    var response = await _vipApi.ObtainVipExperienceAsync(new VipExperienceRequest()
                    {
                        csrf = _biliCookie.BiliJct
                    });
                    if (response.Code != 0)
                    {
                        _logger.LogInformation("大会员经验领取失败，错误信息：{message}", response.Message);
                        break;
                    }

                    _logger.LogInformation("领取成功，经验+10 √");
                    break;

                default:
                    _logger.LogDebug("大会员经验领取失败，未知错误");
                    break;
            }
        }
    }

    [TaskInterceptor("测试Cookie")]
    private async Task<UserInfo> GetUserInfo()
    {
        UserInfo userInfo = await _loginDomainService.LoginByCookie();
        if (userInfo == null) throw new Exception("登录失败，请检查Cookie"); //终止流程

        return userInfo;
    }

    [TaskInterceptor("签到", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> Sign(VipTaskInfo info)
    {
        if (info.Task_info.Sing_task_item.IsTodaySigned)
        {
            _logger.LogInformation("已完成，跳过");
            _logger.LogInformation("今日获得签到积分：{score}", info.Task_info.Sing_task_item.TodayHistory?.Score);
            _logger.LogInformation("累计签到{count}天", info.Task_info.Sing_task_item.Count);
            return info;
        }

        var re = await _vipApi.SignAsync(new SignRequest());
        if (re.Code != 0) throw new Exception(re.ToJsonStr());

        //确认
        var infoResult = await _vipApi.GetTaskListAsync();
        if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
        info = infoResult.Data;

        _logger.LogInformation("今日可获得签到积分：{score}", info.Task_info.Sing_task_item.TodayHistory?.Score);
        _logger.LogInformation(info.Task_info.Sing_task_item.IsTodaySigned ? "签到成功" : "签到失败");
        _logger.LogInformation("累计签到{count}天", info.Task_info.Sing_task_item.Count);

        return info;
    }

    [TaskInterceptor("福利任务", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> Bonus(VipTaskInfo info)
    {
        const string moduleCode = "福利任务";
        const string taskCode = "bonus";

        var bonusTask = GetTarget(info, moduleCode, taskCode);

        if (bonusTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        //如果状态不等于3，则做
        if (bonusTask.state == 3)
        {
            _logger.LogInformation("已完成，跳过");
            return info;
        }

        //0需要领取
        if (bonusTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(bonusTask.task_code);
        }

        _logger.LogInformation("开始完成任务");
        var re = await Complete(bonusTask.task_code);

        //确认
        if (re)
        {
            var infoResult = await _vipApi.GetTaskListAsync();
            if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
            info = infoResult.Data;
            bonusTask = GetTarget(info, moduleCode, taskCode);

            _logger.LogInformation("确认：{re}", bonusTask.state == 3 && bonusTask.complete_times >= 1);
        }

        return info;
    }

    [TaskInterceptor("体验任务", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> Privilege(VipTaskInfo info)
    {
        const string moduleCode = "体验任务";
        const string taskCode = "privilege";

        var privilegeTask = GetTarget(info, moduleCode, taskCode);

        if (privilegeTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        //如果状态不等于3，则做
        if (privilegeTask.state == 3)
        {
            _logger.LogInformation("已完成，跳过");
            return info;
        }

        //0需要领取
        if (privilegeTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(privilegeTask.task_code);
        }

        _logger.LogInformation("开始完成任务");
        var re = await Complete(privilegeTask.task_code);

        //确认
        if (re)
        {
            var infoResult = await _vipApi.GetTaskListAsync();
            if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
            info = infoResult.Data;
            privilegeTask = GetTarget(info, moduleCode, taskCode);

            _logger.LogInformation("确认：{re}", privilegeTask.state == 3 && privilegeTask.complete_times >= 1);
        }

        return info;
    }

    [TaskInterceptor("浏览追番频道页10秒", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> ViewAnimate(VipTaskInfo info)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "animatetab";

        var code = "jp_channel";

        CommonTaskItem targetTask = GetTarget(info, moduleCode, taskCode);

        if (targetTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            _logger.LogInformation("已完成，跳过");
            return info;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code);
        }

        _logger.LogInformation("开始完成任务");
        var re = await CompleteView(code);

        //确认
        if (re)
        {
            var infoResult = await _vipApi.GetTaskListAsync();
            if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
            info = infoResult.Data;
            targetTask = GetTarget(info, moduleCode, taskCode);

            _logger.LogInformation("确认：{re}", targetTask.state == 3 && targetTask.complete_times >= 1);
        }

        return info;
    }

    [TaskInterceptor("浏览会员购页面10秒", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> ViewVipMall(VipTaskInfo info)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "vipmallview";

        CommonTaskItem targetTask = GetTarget(info, moduleCode, taskCode);

        if (targetTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            _logger.LogInformation("已完成，跳过");
            return info;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code);
        }

        _logger.LogInformation("开始完成任务");
        var re = await _vipMallApi.ViewVipMallAsync(new ViewVipMallRequest()
        {
            Csrf = _biliCookie.BiliJct
        });
        if (re.Code != 0) throw new Exception(re.ToJsonStr());

        //确认
        if (re.Code == 0)
        {
            var infoResult = await _vipApi.GetTaskListAsync();
            if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
            info = infoResult.Data;
            targetTask = GetTarget(info, moduleCode, taskCode);

            _logger.LogInformation("确认：{re}", targetTask.state == 3 && targetTask.complete_times >= 1);
        }

        return info;
    }

    [TaskInterceptor("观看剧集内容", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> ViewVideo(VipTaskInfo info)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "ogvwatchnew";

        CommonTaskItem targetTask = GetTarget(info, moduleCode, taskCode);

        if (targetTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        // 如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            _logger.LogInformation("已完成，跳过");
            return info;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code);
        }

        _logger.LogInformation("开始完成任务");

        // 观看剧集内容
        _logger.LogInformation("api变更，暂未实现");

        return info;
    }

    [TaskInterceptor("购买单点付费影片（仅领取）", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> BuyVipVideo(VipTaskInfo info)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "tvodbuy";

        CommonTaskItem targetTask = GetTarget(info, moduleCode, taskCode);

        if (targetTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        if (targetTask.state is 3 or 1)
        {
            var re = targetTask.state == 1 ? "已领取" : "已完成";
            _logger.LogInformation("{re}，跳过", re);
            return info;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code);
        }

        return info;
    }

    [TaskInterceptor("购买指定会员购商品（仅领取）", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> BuyVipMall(VipTaskInfo info)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "vipmallbuy";

        CommonTaskItem targetTask = GetTarget(info, moduleCode, taskCode);

        if (targetTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        if (targetTask.state is 3 or 1)
        {
            var re = targetTask.state == 1 ? "已领取" : "已完成";
            _logger.LogInformation("{re}，跳过", re);
            return info;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code);
        }

        return info;
    }

    [TaskInterceptor("浏览装扮商城主页", TaskLevel.Two, false)]
    private async Task<VipTaskInfo> ViewDressMall(VipTaskInfo info)
    {
        const string moduleCode = "日常任务";
        const string taskCode = "dress-view";

        //var code = "dress-view";

        CommonTaskItem targetTask = GetTarget(info, moduleCode, taskCode);

        if (targetTask == null)
        {
            _logger.LogInformation("任务失效");
            return info;
        }

        //如果状态不等于3，则做
        if (targetTask.state == 3)
        {
            _logger.LogInformation("已完成，跳过");
            return info;
        }

        //0需要领取
        if (targetTask.state == 0)
        {
            _logger.LogInformation("开始领取任务");
            await TryReceive(targetTask.task_code);
        }

        _logger.LogInformation("开始完成任务");
        var re = await CompleteV2(targetTask.task_code);

        //确认
        if (re)
        {
            var infoResult = await _vipApi.GetTaskListAsync();
            if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
            info = infoResult.Data;
            targetTask = GetTarget(info, moduleCode, taskCode);

            _logger.LogInformation("确认：{re}", targetTask.state == 3 && targetTask.complete_times >= 1);
        }

        return info;
    }

    #region private

    private static CommonTaskItem GetTarget(VipTaskInfo info, string moduleCode, string taskCode)
    {
        var module = info.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        return module?
            .common_task_item
            .FirstOrDefault(x => x.task_code == taskCode);
    }

    /// <summary>
    /// 领取任务
    /// </summary>
    private async Task TryReceive(string taskCode)
    {
        BiliApiResponse re = null;
        try
        {
            var request = new ReceiveOrCompleteTaskRequest(taskCode);
            re = await _vipApi.Receive(request);
            if (re.Code == 0)
                _logger.LogInformation("领取任务成功");
            else
                _logger.LogInformation("领取任务失败：{msg}", re.ToJsonStr());
        }
        catch (Exception e)
        {
            _logger.LogError("领取任务异常");
            _logger.LogError(e.Message + re?.ToJsonStr());
        }
    }

    private async Task<bool> Complete(string taskCode)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await _vipApi.CompleteAsync(request);
        if (re.Code == 0)
        {
            _logger.LogInformation("已完成");
            return true;
        }

        else
        {
            _logger.LogInformation("失败：{msg}", re.ToJsonStr());
            return false;
        }
    }

    private async Task<bool> CompleteV2(string taskCode)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await _vipApi.CompleteV2(request);
        if (re.Code == 0)
        {
            _logger.LogInformation("已完成");
            return true;
        }

        else
        {
            _logger.LogInformation("失败：{msg}", re.ToJsonStr());
            return false;
        }
    }

    private async Task<bool> CompleteView(string code)
    {
        _logger.LogInformation("开始浏览");
        await Task.Delay(10 * 1000);

        var request = new ViewRequest(code);
        var re = await _vipApi.ViewComplete(request);
        if (re.Code == 0)
        {
            _logger.LogInformation("浏览完成");
            return true;
        }

        else
        {
            _logger.LogInformation("浏览失败：{msg}", re.ToJsonStr());
            return false;
        }
    }

    public async Task<bool> WatchBangumi()
    {
        if (_vipBigPointOptions.ViewBangumiList == null || _vipBigPointOptions.ViewBangumiList.Count == 0)
            return false;

        long randomSsid =
            _vipBigPointOptions.ViewBangumiList[new Random().Next(0, _vipBigPointOptions.ViewBangumiList.Count)];

        var res = await GetBangumi(randomSsid);
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
            Mid = long.Parse(_biliCookie.UserId),
            Sid = randomSsid,
            Epid = res.Value.Item2,
            Csrf = _biliCookie.BiliJct,
            Type = 4,
            Sub_type = 1,
            Start_ts = DateTime.Now.ToTimeStamp() - playedTime,
            Played_time = playedTime,
            Realtime = playedTime,
            Real_played_time = playedTime
        };
        BiliApiResponse apiResponse = await _videoApi.UploadVideoHeartbeat(request);
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
    private async Task<(VideoInfoDto, long)?> GetBangumi(long randomSsid)
    {
        try
        {
            if (randomSsid is 0 or long.MinValue)
                return null;
            var bangumiInfo = await _videoApi.GetBangumiBySsid(randomSsid);

            // 从获取的剧集中随机获得其中的一集

            var bangumi = bangumiInfo.Result.episodes[new Random().Next(0, bangumiInfo.Result.episodes.Count)];
            var videoInfo = new VideoInfoDto()
            {
                Bvid = bangumi.bvid,
                Aid = bangumi.aid.ToString(),
                Cid = bangumi.cid,
                Copyright = 1,
                Duration = bangumi.duration,
                Title = bangumi.share_copy
            };
            _logger.LogInformation("本次播放的正片为：{title}", bangumi.share_copy);
            return (videoInfo, bangumi.ep_id);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return null;
    }

    #endregion
}
