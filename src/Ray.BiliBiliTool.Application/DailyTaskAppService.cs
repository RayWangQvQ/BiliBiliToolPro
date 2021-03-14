using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class DailyTaskAppService : AppService, IDailyTaskAppService
    {
        private readonly ILogger<DailyTaskAppService> _logger;
        private readonly IAccountDomainService _loginDomainService;
        private readonly IVideoDomainService _videoDomainService;
        private readonly IDonateCoinDomainService _donateCoinDomainService;
        private readonly IMangaDomainService _mangaDomainService;
        private readonly ILiveDomainService _liveDomainService;
        private readonly IVipPrivilegeDomainService _vipPrivilegeDomainService;
        private readonly IChargeDomainService _chargeDomainService;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly ICoinDomainService _coinDomainService;
        private readonly SecurityOptions _securityOptions;

        public DailyTaskAppService(
            ILogger<DailyTaskAppService> logger,
            IAccountDomainService loginDomainService,
            IVideoDomainService videoDomainService,
            IDonateCoinDomainService donateCoinDomainService,
            IMangaDomainService mangaDomainService,
            ILiveDomainService liveDomainService,
            IVipPrivilegeDomainService vipPrivilegeDomainService,
            IChargeDomainService chargeDomainService,
            IOptionsMonitor<SecurityOptions> securityOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            ICoinDomainService coinDomainService
        )
        {
            _logger = logger;
            _loginDomainService = loginDomainService;
            _videoDomainService = videoDomainService;
            _donateCoinDomainService = donateCoinDomainService;
            _mangaDomainService = mangaDomainService;
            _liveDomainService = liveDomainService;
            _vipPrivilegeDomainService = vipPrivilegeDomainService;
            _chargeDomainService = chargeDomainService;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _coinDomainService = coinDomainService;
            _securityOptions = securityOptions.CurrentValue;
        }

        public override string TaskName => "Daily";

        public override void DoTask()
        {
            _logger.LogInformation("-----开始每日任务-----\r\n");

            UserInfo userInfo = Login();
            DailyTaskInfo dailyTaskInfo = GetDailyTaskStatus();

            WatchAndShareVideo(dailyTaskInfo);
            AddCoinsForVideo();
            MangaSign();
            LiveSign();
            ExchangeSilver2Coin();

            ReceiveVipPrivilege(ref userInfo);
            ReceiveMangaVipReward(userInfo);
            Charge(userInfo);

            _logger.LogInformation("-----每日任务全部已执行结束-----\r\n");
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor("登录")]
        private UserInfo Login()
        {
            UserInfo userInfo = _loginDomainService.LoginByCookie();
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
            return _loginDomainService.GetDailyTaskStatus();
        }

        /// <summary>
        /// 观看、分享视频
        /// </summary>
        [TaskInterceptor("观看、分享视频", false)]
        private void WatchAndShareVideo(DailyTaskInfo dailyTaskInfo)
        {
            if (!_dailyTaskOptions.IsWatchVideo && !_dailyTaskOptions.IsShareVideo)
            {
                _logger.LogInformation("已配置为关闭，跳过任务");
                return;
            }
            _videoDomainService.WatchAndShareVideo(dailyTaskInfo);
        }

        /// <summary>
        /// 投币任务
        /// </summary>
        [TaskInterceptor("投币", false)]
        private void AddCoinsForVideo()
        {
            _donateCoinDomainService.AddCoinsForVideos();
        }

        /// <summary>
        /// 直播中心签到
        /// </summary>
        [TaskInterceptor("直播中心签到", false)]
        private void LiveSign()
        {
            _liveDomainService.LiveSign();
        }

        /// <summary>
        /// 直播中心的银瓜子兑换硬币
        /// </summary>
        [TaskInterceptor("直播中心银瓜子兑换硬币", false)]
        private void ExchangeSilver2Coin()
        {
            var success = _liveDomainService.ExchangeSilver2Coin();
            if (!success) return;

            //如果兑换成功，则打印硬币余额
            var coinBalance = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("当前硬币余额: {0}", coinBalance);
        }

        /// <summary>
        /// 每月领取大会员福利
        /// </summary>
        [TaskInterceptor("每月领取大会员福利", false)]
        private void ReceiveVipPrivilege(ref UserInfo userInfo)
        {
            var suc = _vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo);

            //如果领取成功，需要刷新账户信息（比如B币余额）
            if (suc)
            {
                try
                {
                    userInfo = _loginDomainService.LoginByCookie();
                }
                catch (Exception ex)
                {
                    _logger.LogError("领取福利成功，但之后刷新用户信息时异常，信息：{msg}", ex.Message);
                }
            }
        }

        /// <summary>
        /// 每月为自己充电
        /// </summary>
        [TaskInterceptor("每月使用快过期的B币充电", false)]
        private void Charge(UserInfo userInfo)
        {
            _chargeDomainService.Charge(userInfo);
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        [TaskInterceptor("漫画签到", false)]
        private void MangaSign()
        {
            _mangaDomainService.MangaSign();
        }

        /// <summary>
        /// 每月获取大会员漫画权益
        /// </summary>
        [TaskInterceptor("每月领取大会员漫画权益", false)]
        private void ReceiveMangaVipReward(UserInfo userInfo)
        {
            _mangaDomainService.ReceiveMangaVipReward(1, userInfo);
        }
    }
}
