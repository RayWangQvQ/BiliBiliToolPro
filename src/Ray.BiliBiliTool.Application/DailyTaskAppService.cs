using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application
{
    public class DailyTaskAppService : AppService, IDailyTaskAppService
    {
        private readonly ILogger<DailyTaskAppService> _logger;
        private readonly IAccountDomainService _accountDomainService;
        private readonly IVideoDomainService _videoDomainService;
        private readonly IArticleDomainService _articleDomainService;
        private readonly IDonateCoinDomainService _donateCoinDomainService;
        private readonly IMangaDomainService _mangaDomainService;
        private readonly ILiveDomainService _liveDomainService;
        private readonly IVipPrivilegeDomainService _vipPrivilegeDomainService;
        private readonly IChargeDomainService _chargeDomainService;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly ICoinDomainService _coinDomainService;
        private readonly Dictionary<string, int> _expDic;
        private readonly ILoginDomainService _loginDomainService;
        private readonly IConfiguration _configuration;
        private readonly CookieStrFactory _cookieStrFactory;
        private BiliCookie _biliCookie;

        public DailyTaskAppService(
            ILogger<DailyTaskAppService> logger,
            IOptionsMonitor<Dictionary<string, int>> dicOptions,
            IAccountDomainService accountDomainService,
            IVideoDomainService videoDomainService,
            IArticleDomainService articleDomainService,
            IDonateCoinDomainService donateCoinDomainService,
            IMangaDomainService mangaDomainService,
            ILiveDomainService liveDomainService,
            IVipPrivilegeDomainService vipPrivilegeDomainService,
            IChargeDomainService chargeDomainService,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            ICoinDomainService coinDomainService,
            ILoginDomainService loginDomainService, IConfiguration configuration,
            CookieStrFactory cookieStrFactory,
            BiliCookie biliCookie)
        {
            _logger = logger;
            _expDic = dicOptions.Get(Constants.OptionsNames.ExpDictionaryName);
            _accountDomainService = accountDomainService;
            _videoDomainService = videoDomainService;
            _articleDomainService = articleDomainService;
            _donateCoinDomainService = donateCoinDomainService;
            _mangaDomainService = mangaDomainService;
            _liveDomainService = liveDomainService;
            _vipPrivilegeDomainService = vipPrivilegeDomainService;
            _chargeDomainService = chargeDomainService;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _coinDomainService = coinDomainService;
            _loginDomainService = loginDomainService;
            _configuration = configuration;
            _cookieStrFactory = cookieStrFactory;
            _biliCookie = biliCookie;
        }

        [TaskInterceptor("每日任务", TaskLevel.One)]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            await SetCookiesAsync(_biliCookie, cancellationToken);

            //每日任务赚经验：
            UserInfo userInfo = await Login();

            DailyTaskInfo dailyTaskInfo = await GetDailyTaskStatus();
            await WatchAndShareVideo(dailyTaskInfo);

            await AddCoins(userInfo);

            //签到：
            await LiveSign();
            await MangaSign();
            await MangaRead();
            await ExchangeSilver2Coin();

            //领福利：
            await ReceiveVipPrivilege(userInfo);
            await ReceiveMangaVipReward(userInfo);

            //TODO 大会员领经验


            await Charge(userInfo);
        }


        [TaskInterceptor("Set Cookie", TaskLevel.Two)]
        protected async Task<BiliCookie> SetCookiesAsync(BiliCookie biliCookie, CancellationToken cancellationToken)
        {
            //判断cookie是否完整
            if (biliCookie.Buvid.IsNotNullOrEmpty())
            {
                _logger.LogInformation("Cookie完整，不需要Set Cookie");
                return biliCookie;
            }

            //Set
            _logger.LogInformation("开始Set Cookie");
            var ck = await _loginDomainService.SetCookieAsync(biliCookie, cancellationToken);

            //持久化
            _logger.LogInformation("持久化Cookie");
            await SaveCookieAsync(ck, cancellationToken);

            return ck;
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor("登录")]
        private async Task<UserInfo> Login()
        {
            UserInfo userInfo = await _accountDomainService.LoginByCookie();
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie"); //终止流程

            _expDic.TryGetValue("每日登录", out int exp);
            _logger.LogInformation("登录成功，经验+{exp} √", exp);

            return userInfo;
        }

        /// <summary>
        /// 获取任务完成情况
        /// </summary>
        /// <returns></returns>
        [TaskInterceptor(null, rethrowWhenException: false)]
        private async Task<DailyTaskInfo> GetDailyTaskStatus()
        {
            return await _accountDomainService.GetDailyTaskStatus();
        }

        /// <summary>
        /// 观看、分享视频
        /// </summary>
        [TaskInterceptor("观看、分享视频", rethrowWhenException: false)]
        private async Task WatchAndShareVideo(DailyTaskInfo dailyTaskInfo)
        {
            if (!_dailyTaskOptions.IsWatchVideo && !_dailyTaskOptions.IsShareVideo)
            {
                _logger.LogInformation("已配置为关闭，跳过任务");
                return;
            }

            await _videoDomainService.WatchAndShareVideo(dailyTaskInfo);
        }

        /// <summary>
        /// 投币任务
        /// </summary>
        [TaskInterceptor("投币", rethrowWhenException: false)]
        private async Task AddCoins(UserInfo userInfo)
        {
            if (_dailyTaskOptions.SaveCoinsWhenLv6 && userInfo.Level_info.Current_level >= 6)
            {
                _logger.LogInformation("已经为LV6大佬，开始白嫖");
                return;
            }

            if (_dailyTaskOptions.IsDonateCoinForArticle)
            {
                _logger.LogInformation("专栏投币已开启");

                if (!await _articleDomainService.AddCoinForArticles())
                {
                    _logger.LogInformation("专栏投币结束，转入视频投币");
                    await _donateCoinDomainService.AddCoinsForVideos();
                }
            }
            else
            {
                await _donateCoinDomainService.AddCoinsForVideos();
            }
        }

        /// <summary>
        /// 直播中心签到
        /// </summary>
        [TaskInterceptor("直播签到", rethrowWhenException: false)]
        private async Task LiveSign()
        {
            await _liveDomainService.LiveSign();
        }

        /// <summary>
        /// 直播中心的银瓜子兑换硬币
        /// </summary>
        [TaskInterceptor("银瓜子兑换硬币", rethrowWhenException: false)]
        private async Task ExchangeSilver2Coin()
        {
            var success = await _liveDomainService.ExchangeSilver2Coin();
            if (!success) return;

            //如果兑换成功，则打印硬币余额
            var coinBalance = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("【硬币余额】 {coin}", coinBalance);
        }

        /// <summary>
        /// 每月领取大会员福利
        /// </summary>
        [TaskInterceptor("领取大会员福利", rethrowWhenException: false)]
        private async Task ReceiveVipPrivilege(UserInfo userInfo)
        {
            var suc = await _vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo);

            //如果领取成功，需要刷新账户信息（比如B币余额）
            if (suc)
            {
                try
                {
                    userInfo = await _accountDomainService.LoginByCookie();
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
        private async Task Charge(UserInfo userInfo)
        {
            await _chargeDomainService.Charge(userInfo);
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        [TaskInterceptor("漫画签到", rethrowWhenException: false)]
        private async Task MangaSign()
        {
            await _mangaDomainService.MangaSign();
        }

        /// <summary>
        /// 漫画阅读
        /// </summary>
        [TaskInterceptor("漫画阅读", rethrowWhenException: false)]
        private async Task MangaRead()
        {
            await _mangaDomainService.MangaRead();
        }

        /// <summary>
        /// 每月获取大会员漫画权益
        /// </summary>
        [TaskInterceptor("领取大会员漫画权益", rethrowWhenException: false)]
        private async Task ReceiveMangaVipReward(UserInfo userInfo)
        {
            await _mangaDomainService.ReceiveMangaVipReward(1, userInfo);
        }

        private async Task SaveCookieAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
        {
            var platformType = _configuration.GetSection("PlateformType").Get<PlatformType>();
            _logger.LogInformation("当前运行平台：{platform}", platformType);

            //更新cookie到青龙env
            if (platformType == PlatformType.QingLong)
            {
                await _loginDomainService.SaveCookieToQinLongAsync(ckInfo, cancellationToken);
                return;
            }

            //更新cookie到json
            await _loginDomainService.SaveCookieToJsonFileAsync(ckInfo, cancellationToken);
        }
    }
}
