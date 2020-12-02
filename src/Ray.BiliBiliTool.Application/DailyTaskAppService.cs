using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

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
        private readonly SecurityOptions _securityOptions;

        public DailyTaskAppService(
            ILogger<DailyTaskAppService> logger,
            IAccountDomainService loginDomainService,
            IVideoDomainService videoDomainService,
            IMangaDomainService mangaDomainService,
            ILiveDomainService liveDomainService,
            IVipPrivilegeDomainService vipPrivilegeDomainService,
            IChargeDomainService chargeDomainService,
            IOptionsMonitor<SecurityOptions> securityOptions)
        {
            this._logger = logger;
            this._loginDomainService = loginDomainService;
            this._videoDomainService = videoDomainService;
            this._mangaDomainService = mangaDomainService;
            this._liveDomainService = liveDomainService;
            this._vipPrivilegeDomainService = vipPrivilegeDomainService;
            this._chargeDomainService = chargeDomainService;
            this._securityOptions = securityOptions.CurrentValue;
        }

        public void DoDailyTask()
        {
            if (this._securityOptions.IsSkipDailyTask)
            {
                this._logger.LogWarning("已配置为跳过每日任务");
                return;
            }

            this._logger.LogInformation("-----开始每日任务-----\r\n");

            UseInfo userInfo;
            DailyTaskInfo dailyTaskInfo;

            userInfo = this.Login();
            dailyTaskInfo = this.GetDailyTaskStatus();

            this.WatchAndShareVideo(dailyTaskInfo);
            this.AddCoinsForVideo();
            this.MangaSign();
            this.LiveSign();
            userInfo.Money = this.ExchangeSilver2Coin();

            this.ReceiveVipPrivilege(userInfo);
            this.ReceiveMangaVipReward(userInfo);
            this.Charge(userInfo);

            this._logger.LogInformation("-----全部任务已执行结束-----\r\n");
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor("登录")]
        private UseInfo Login()
        {
            UseInfo userInfo = this._loginDomainService.LoginByCookie();
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie");//终止流程
            return userInfo;
        }

        /// <summary>
        /// 获取任务完成情况
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor(null, false)]
        private DailyTaskInfo GetDailyTaskStatus()
        {
            return this._loginDomainService.GetDailyTaskStatus();
        }

        /// <summary>
        /// 观看、分享视频
        /// </summary>
        [TaskInterceptor("观看、分享视频", false)]
        private void WatchAndShareVideo(DailyTaskInfo dailyTaskInfo)
        {
            this._videoDomainService.WatchAndShareVideo(dailyTaskInfo);
        }

        /// <summary>
        /// 投币任务
        /// </summary>
        [TaskInterceptor("投币", false)]
        private void AddCoinsForVideo()
        {
            this._videoDomainService.AddCoinsForVideo();
        }

        /// <summary>
        /// 直播中心签到
        /// </summary>
        [TaskInterceptor("直播中心签到", false)]
        private void LiveSign()
        {
            this._liveDomainService.LiveSign();
        }

        /// <summary>
        /// 直播中心的银瓜子兑换硬币
        /// </summary>
        [TaskInterceptor("直播中心银瓜子兑换硬币", false)]
        private decimal ExchangeSilver2Coin()
        {
            return this._liveDomainService.ExchangeSilver2Coin();
        }

        /// <summary>
        /// 每月领取大会员福利
        /// </summary>
        [TaskInterceptor("每月领取大会员福利", false)]
        private void ReceiveVipPrivilege(UseInfo userInfo)
        {
            this._vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo);
        }

        /// <summary>
        /// 每月为自己充电
        /// </summary>
        [TaskInterceptor("每月为自己充电", false)]
        private void Charge(UseInfo userInfo)
        {
            this._chargeDomainService.Charge(userInfo);
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        [TaskInterceptor("漫画签到", false)]
        private void MangaSign()
        {
            this._mangaDomainService.MangaSign();
        }

        /// <summary>
        /// 每月获取大会员漫画权益
        /// </summary>
        [TaskInterceptor("每月领取大会员漫画权益", false)]
        private void ReceiveMangaVipReward(UseInfo userInfo)
        {
            this._mangaDomainService.ReceiveMangaVipReward(1, userInfo);
        }
    }
}
