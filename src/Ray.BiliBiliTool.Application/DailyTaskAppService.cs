using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
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
        private readonly Dictionary<string, int> _expDic;

        public DailyTaskAppService(
            ILogger<DailyTaskAppService> logger,
            IOptionsMonitor<Dictionary<string, int>> dicOptions,
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
            _expDic = dicOptions.Get(Constants.OptionsNames.ExpDictionaryName);
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

        [TaskInterceptor("每日任务", TaskLevel.One)]
        public override void DoTask()
        {
            //每日任务赚经验：
            UserInfo userInfo = Login();
            DailyTaskInfo dailyTaskInfo = GetDailyTaskStatus();
            WatchAndShareVideo(dailyTaskInfo);
            AddCoinsForVideo();

            //签到：
            LiveSign();
            MangaSign();

            ExchangeSilver2Coin();

            //领福利：
            ReceiveVipPrivilege(ref userInfo);
            ReceiveMangaVipReward(userInfo);

            Charge(userInfo);
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

            _expDic.TryGetValue("每日登录", out int exp);
            _logger.LogInformation("登录成功，经验+{exp} √", exp);

            return userInfo;
        }

        /// <summary>
        /// 获取任务完成情况
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor(null, rethrowWhenException: false)]
        private DailyTaskInfo GetDailyTaskStatus()
        {
            return _loginDomainService.GetDailyTaskStatus();
        }

        /// <summary>
        /// 观看、分享视频
        /// </summary>
        [TaskInterceptor("观看、分享视频", rethrowWhenException: false)]
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
        [TaskInterceptor("投币", rethrowWhenException: false)]
        private void AddCoinsForVideo()
        {
            _donateCoinDomainService.AddCoinsForVideos();
        }

        /// <summary>
        /// 直播中心签到
        /// </summary>
        [TaskInterceptor("直播签到", rethrowWhenException: false)]
        private void LiveSign()
        {
            _liveDomainService.LiveSign();
        }

        /// <summary>
        /// 直播中心的银瓜子兑换硬币
        /// </summary>
        [TaskInterceptor("银瓜子兑换硬币", rethrowWhenException: false)]
        private void ExchangeSilver2Coin()
        {
            var success = _liveDomainService.ExchangeSilver2Coin();
            if (!success) return;

            //如果兑换成功，则打印硬币余额
            var coinBalance = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("【硬币余额】 {0}", coinBalance);
        }

        /// <summary>
        /// 每月领取大会员福利
        /// </summary>
        [TaskInterceptor("领取大会员福利", rethrowWhenException: false)]
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
        [TaskInterceptor("B币券充电", rethrowWhenException: false)]
        private void Charge(UserInfo userInfo)
        {
            _chargeDomainService.Charge(userInfo);
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        [TaskInterceptor("漫画签到", rethrowWhenException: false)]
        private void MangaSign()
        {
            _mangaDomainService.MangaSign();
        }

        /// <summary>
        /// 每月获取大会员漫画权益
        /// </summary>
        [TaskInterceptor("领取大会员漫画权益", rethrowWhenException: false)]
        private void ReceiveMangaVipReward(UserInfo userInfo)
        {
            _mangaDomainService.ReceiveMangaVipReward(1, userInfo);
        }
    }
}
