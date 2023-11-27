using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class VipBigPointAppService : AppService, IVipBigPointAppService
    {
        private readonly ILogger<VipBigPointAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IVipBigPointApi _vipApi;
        private readonly IAccountDomainService _loginDomainService;
        private readonly IVideoDomainService _videoDomainService;
        private readonly IAccountDomainService _accountDomainService;
        private readonly BiliCookie _biliCookie;

        public VipBigPointAppService(
            IConfiguration configuration,
            ILogger<VipBigPointAppService> logger,
            IVipBigPointApi vipApi,
            IAccountDomainService loginDomainService,
            IVideoDomainService videoDomainService,
            BiliCookie biliCookie, IAccountDomainService accountDomainService)
        {
            _configuration = configuration;
            _logger = logger;
            _vipApi = vipApi;
            _loginDomainService = loginDomainService;
            _videoDomainService = videoDomainService;
            _biliCookie = biliCookie;
            _accountDomainService = accountDomainService;
        }

        public async Task VipExpress()
        {
            _logger.LogInformation("大会员经验领取任务开始");
            var re = await _vipApi.GetVouchersInfo();
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
                        var response = await _vipApi.GetVipExperience(new VipExperienceRequest()
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


        [TaskInterceptor("大会员大积分", TaskLevel.One)]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            await VipExpress();

            // TODO 解决taskInfo在一个任务出错后，后续的任务均会报空引用错误
            var ui = await GetUserInfo();

            if (ui.GetVipType() == VipType.None)
            {
                _logger.LogInformation("当前不是大会员或已过期，跳过任务");
                return;
            }

            var re = await _vipApi.GetTaskList();

            if (re.Code != 0) throw new Exception(re.ToJsonStr());

            VipTaskInfo taskInfo = re.Data;
            taskInfo.LogInfo(_logger);

            //签到
            taskInfo = await Sign(taskInfo);

            //福利任务
            taskInfo = await Bonus(taskInfo);

            //体验任务
            taskInfo = await Privilege(taskInfo);

            //日常任务

            //浏览追番频道页10秒
            taskInfo = await ViewAnimate(taskInfo);

            //浏览影视频道页10秒
            // taskInfo = await ViewFilmChannel(taskInfo);

            //浏览会员购页面10秒
            taskInfo = ViewVipMall(taskInfo);

            //观看任意正片内容
            taskInfo = await ViewVideo(taskInfo);

            //领取购买任务
            taskInfo = await BuyVipVideo(taskInfo);
            // taskInfo = await BuyVipProduct(taskInfo);
            taskInfo = await BuyVipMall(taskInfo);
                        
            taskInfo.LogInfo(_logger);

            
        }

        [TaskInterceptor("测试Cookie")]
        private async Task<UserInfo> GetUserInfo()
        {
            UserInfo userInfo = await _loginDomainService.LoginByCookie();
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie");//终止流程

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

            var re = await _vipApi.Sign(new SignRequest());
            if (re.Code != 0) throw new Exception(re.ToJsonStr());

            //确认
            var infoResult = await _vipApi.GetTaskList();
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
            var bonusTask = GetTarget(info);

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
                var infoResult = await _vipApi.GetTaskList();
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
                info = infoResult.Data;
                bonusTask = GetTarget(info);

                _logger.LogInformation("确认：{re}", bonusTask.state == 3 && bonusTask.complete_times >= 1);
            }

            return info;

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "福利任务")
                    .common_task_item
                    .First(x => x.task_code == "bonus");
            }
        }

        [TaskInterceptor("体验任务", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> Privilege(VipTaskInfo info)
        {
            var privilegeTask = GetTarget(info);

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
                var infoResult = await _vipApi.GetTaskList();
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
                info = infoResult.Data;
                privilegeTask = GetTarget(info);

                _logger.LogInformation("确认：{re}", privilegeTask.state == 3 && privilegeTask.complete_times >= 1);
            }

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "体验任务")
                    .common_task_item
                    .First(x => x.task_code == "privilege");
            }

            return info;
        }

        [TaskInterceptor("浏览追番频道页10秒", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> ViewAnimate(VipTaskInfo info)
        {
            var code = "jp_channel";

            CommonTaskItem targetTask = GetTarget(info);

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
                var infoResult = await _vipApi.GetTaskList();
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
                info = infoResult.Data;
                targetTask = GetTarget(info);

                _logger.LogInformation("确认：{re}", targetTask.state == 3 && targetTask.complete_times >= 1);
            }

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "日常任务")
                    .common_task_item
                    .First(x => x.task_code == "animatetab");
            }

            return info;
        }

        [TaskInterceptor("浏览影视频道页10秒", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> ViewFilmChannel(VipTaskInfo info)
        {
            var code = "tv_channel";

            CommonTaskItem targetTask = GetTarget(info);

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
                var infoResult = await _vipApi.GetTaskList();
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJsonStr());
                info = infoResult.Data;
                targetTask = GetTarget(info);

                _logger.LogInformation("确认：{re}", targetTask.state == 3 && targetTask.complete_times >= 1);
            }

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "日常任务")
                    .common_task_item
                    .First(x => x.task_code == "filmtab");
            }

            return info;
        }

        [TaskInterceptor("浏览会员购页面10秒", TaskLevel.Two, false)]
        private VipTaskInfo ViewVipMall(VipTaskInfo info)
        {
            //todo
            _logger.LogInformation("待实现...");
            return info;
        }

        [TaskInterceptor("观看任意正片内容", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> ViewVideo(VipTaskInfo info)
        {
            CommonTaskItem targetTask = GetTarget(info);

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
            _logger.LogInformation("待开发...");//todo

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "日常任务")
                    .common_task_item
                    .First(x => x.task_code == "ogvwatch");
            }

            return info;
        }

        [TaskInterceptor("购买单点付费影片（仅领取）", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> BuyVipVideo(VipTaskInfo info)
        {
            CommonTaskItem targetTask = GetTarget(info);

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

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "日常任务")
                    .common_task_item
                    .First(x => x.task_code == "tvodbuy");
            }
        }

        [TaskInterceptor("购买指定大会员产品（仅领取）", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> BuyVipProduct(VipTaskInfo info)
        {
            CommonTaskItem targetTask = GetTarget(info);

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

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "日常任务")
                    .common_task_item
                    .First(x => x.task_code == "subscribe");
            }
        }

        [TaskInterceptor("购买指定会员购商品（仅领取）", TaskLevel.Two, false)]
        private async Task<VipTaskInfo> BuyVipMall(VipTaskInfo info)
        {
            CommonTaskItem targetTask = GetTarget(info);

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

            CommonTaskItem GetTarget(VipTaskInfo info)
            {
                return info.Task_info.Modules.First(x => x.module_title == "日常任务")
                    .common_task_item
                    .First(x => x.task_code == "vipmallbuy");
            }
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
            var re = await _vipApi.Complete(request);
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
    }
}
