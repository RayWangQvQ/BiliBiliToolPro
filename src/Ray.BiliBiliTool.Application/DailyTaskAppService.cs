using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Extensions;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Application
{
    public class DailyTaskAppService : IDailyTaskAppService
    {
        private readonly ILogger<DailyTaskAppService> _logger;
        private readonly IAccountDomainService _loginDomainService;
        private readonly IVideoDomainService _videoDomainService;
        private readonly IMangaDomainService _mangaDomainService;
        private readonly ILiveDomainService _liveDomainService;
        private readonly IVipPrivilegeDomainService _vipPrivilegeDomainService;
        private readonly IChargeDomainService _chargeDomainService;

        //AppendPushMsg desp = AppendPushMsg.getInstance();

        public DailyTaskAppService(
            ILogger<DailyTaskAppService> logger,
            IAccountDomainService loginDomainService,
            IVideoDomainService videoDomainService,
            IMangaDomainService mangaDomainService,
            ILiveDomainService liveDomainService,
            IVipPrivilegeDomainService vipPrivilegeDomainService,
            IChargeDomainService chargeDomainService)
        {
            _logger = logger;
            _loginDomainService = loginDomainService;
            _videoDomainService = videoDomainService;
            _mangaDomainService = mangaDomainService;
            _liveDomainService = liveDomainService;
            _vipPrivilegeDomainService = vipPrivilegeDomainService;
            _chargeDomainService = chargeDomainService;
        }

        /// <summary>
        /// 当前登录账户信息缓存
        /// </summary>
        private UseInfo LoginResponse { get; set; }


        public void DoDailyTask()
        {
            //登录
            var userInfo = _loginDomainService.LoginByCookie();
            LoginResponse = userInfo;

            DailyTaskInfo dailyTaskStatus = _loginDomainService.GetDailyTaskStatus();
            string videoAid = _videoDomainService.GetRandomVideo();

            //观看视频
            if (!dailyTaskStatus.Watch)
                _videoDomainService.WatchVideo(videoAid);
            else
                _logger.LogInformation("本日观看视频任务已经完成了，不需要再观看视频了");

            //分享视频
            if (!dailyTaskStatus.Share)
                _videoDomainService.ShareVideo(videoAid);
            else
                _logger.LogInformation("本日分享视频任务已经完成了，不需要再分享视频了");

            //投币任务
            _videoDomainService.AddCoinsForVideo();//todo:传入up主Id，只为指定ups投币

            //直播中心签到
            _liveDomainService.LiveSign();

            //直播中心的银瓜子兑换硬币
            int coinTotal = _liveDomainService.ExchangeSilver2Coin();
            LoginResponse.Money = coinTotal;

            //月初领取大会员福利
            _vipPrivilegeDomainService.ReceiveVipPrivilege(LoginResponse);

            //月底充电
            _chargeDomainService.Charge(LoginResponse);

            //漫画签到
            _mangaDomainService.MangaSign();
            //获取每月大会员漫画权益
            _mangaDomainService.ReceiveMangaVipReward(1, LoginResponse);

            _logger.LogInformation("本日任务已全部执行完毕");

            /*
            doServerPush();
            */
        }

        #region 
        //public void doServerPush()
        //{
        //    if (ServerVerify.getMsgPushKey() != null)
        //    {
        //        ServerPush serverPush = new ServerPush();
        //        serverPush.pushMsg("BILIBILIHELPER任务简报", desp.getPushDesp());
        //    }
        //    else
        //    {
        //        _logger.LogInformation("未配置server酱,本次执行不推送日志到微信");
        //    }

        //}
        #endregion
    }
}
