using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        public VipBigPointAppService(
            IConfiguration configuration,
            ILogger<VipBigPointAppService> logger,
            IVipBigPointApi vipApi,
            IAccountDomainService loginDomainService
            )
        {
            _configuration = configuration;
            _logger = logger;
            _vipApi = vipApi;
            _loginDomainService = loginDomainService;
        }

        [TaskInterceptor("大会员大积分", TaskLevel.One)]
        public override void DoTask()
        {
            var ui = GetUserInfo();

            if (ui.GetVipType() == VipType.None)
            {
                _logger.LogInformation("当前不是大会员或已过期，跳过任务");
                return;
            }

            var re = _vipApi.GetTaskList().Result;

            if (re.Code != 0) throw new Exception(re.ToJson());

            VipTaskInfo taskInfo = re.Data;
            taskInfo.LogInfo(_logger);

            //签到
            taskInfo = Sign(taskInfo);

            //福利任务
            taskInfo = Bonus(taskInfo);

            //体验任务
            taskInfo = Privilege(taskInfo);

            //日常任务

            //浏览追番频道页10秒
            taskInfo = ViewAnimate(taskInfo);

            //浏览影视频道页10秒
            taskInfo = ViewFilmChannel(taskInfo);

            //浏览会员购页面10秒
            taskInfo = ViewVipMall(taskInfo);

            //观看任意正片内容
            taskInfo = ViewVideo(taskInfo);

            //领取购买任务
            taskInfo = BuyVipVideo(taskInfo);
            taskInfo = BuyVipProduct(taskInfo);
            taskInfo = BuyVipMall(taskInfo);

            taskInfo.LogInfo(_logger);
        }

        [TaskInterceptor("测试Cookie")]
        private UserInfo GetUserInfo()
        {
            UserInfo userInfo = _loginDomainService.LoginByCookie();
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie");//终止流程

            return userInfo;
        }

        [TaskInterceptor("签到", TaskLevel.Two, false)]
        private VipTaskInfo Sign(VipTaskInfo info)
        {
            if (info.Task_info.Sing_task_item.IsTodaySigned)
            {
                _logger.LogInformation("已完成，跳过");
                _logger.LogInformation("今日获得签到积分：{score}", info.Task_info.Sing_task_item.TodayHistory?.Score);
                _logger.LogInformation("累计签到{count}天", info.Task_info.Sing_task_item.Count);
                return info;
            }

            var re = _vipApi.Sign(new SignRequest()).Result;
            if (re.Code != 0) throw new Exception(re.ToJson());

            //确认
            var infoResult = _vipApi.GetTaskList().Result;
            if (infoResult.Code != 0) throw new Exception(infoResult.ToJson());
            info = infoResult.Data;

            _logger.LogInformation("今日可获得签到积分：{score}", info.Task_info.Sing_task_item.TodayHistory?.Score);
            _logger.LogInformation(info.Task_info.Sing_task_item.IsTodaySigned ? "签到成功" : "签到失败");
            _logger.LogInformation("累计签到{count}天", info.Task_info.Sing_task_item.Count);

            return info;
        }

        [TaskInterceptor("福利任务", TaskLevel.Two, false)]
        private VipTaskInfo Bonus(VipTaskInfo info)
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
                TryReceive(bonusTask.task_code);
            }

            _logger.LogInformation("开始完成任务");
            var re = Complete(bonusTask.task_code);

            //确认
            if (re)
            {
                var infoResult = _vipApi.GetTaskList().Result;
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJson());
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
        private VipTaskInfo Privilege(VipTaskInfo info)
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
                TryReceive(privilegeTask.task_code);
            }

            _logger.LogInformation("开始完成任务");
            var re = Complete(privilegeTask.task_code);

            //确认
            if (re)
            {
                var infoResult = _vipApi.GetTaskList().Result;
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJson());
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
        private VipTaskInfo ViewAnimate(VipTaskInfo info)
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
                TryReceive(targetTask.task_code);
            }

            _logger.LogInformation("开始完成任务");
            var re = CompleteView(code);

            //确认
            if (re)
            {
                var infoResult = _vipApi.GetTaskList().Result;
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJson());
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
        private VipTaskInfo ViewFilmChannel(VipTaskInfo info)
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
                TryReceive(targetTask.task_code);
            }

            _logger.LogInformation("开始完成任务");
            var re = CompleteView(code);

            //确认
            if (re)
            {
                var infoResult = _vipApi.GetTaskList().Result;
                if (infoResult.Code != 0) throw new Exception(infoResult.ToJson());
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
        private VipTaskInfo ViewVideo(VipTaskInfo info)
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
                TryReceive(targetTask.task_code);
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
        private VipTaskInfo BuyVipVideo(VipTaskInfo info)
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
                TryReceive(targetTask.task_code);
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
        private VipTaskInfo BuyVipProduct(VipTaskInfo info)
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
                TryReceive(targetTask.task_code);
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
        private VipTaskInfo BuyVipMall(VipTaskInfo info)
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
                TryReceive(targetTask.task_code);
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
        private void TryReceive(string taskCode)
        {
            BiliApiResponse re = null;
            try
            {
                var request = new ReceiveOrCompleteTaskRequest(taskCode);
                re = _vipApi.Receive(request).Result;
                if (re.Code == 0)
                    _logger.LogInformation("领取任务成功");
                else
                    _logger.LogInformation("领取任务失败：{msg}", re.ToJson());
            }
            catch (Exception e)
            {
                _logger.LogError("领取任务异常");
                _logger.LogError(e.Message + re?.ToJson());
            }
        }

        private bool Complete(string taskCode)
        {
            var request = new ReceiveOrCompleteTaskRequest(taskCode);
            var re = _vipApi.Complete(request).Result;
            if (re.Code == 0)
            {
                _logger.LogInformation("已完成");
                return true;
            }

            else
            {
                _logger.LogInformation("失败：{msg}", re.ToJson());
                return false;
            }
        }

        private bool CompleteView(string code)
        {
            _logger.LogInformation("开始浏览");
            Task.Delay(10 * 1000).Wait();

            var request = new ViewRequest(code);
            var re = _vipApi.ViewComplete(request).Result;
            if (re.Code == 0)
            {
                _logger.LogInformation("浏览完成");
                return true;
            }

            else
            {
                _logger.LogInformation("浏览失败：{msg}", re.ToJson());
                return false;
            }
        }
    }
}
