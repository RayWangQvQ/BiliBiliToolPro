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
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

public class DailyTaskAppService(
    ILogger<DailyTaskAppService> logger,
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
    ILoginDomainService loginDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory
) : BaseMultiAccountsAppService(logger, cookieStrFactory), IDailyTaskAppService
{
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly Dictionary<string, int> _expDic = Config.Constants.ExpDic;

    [TaskInterceptor("每日任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await SetCookiesAsync(ck, cancellationToken);

        //每日任务赚经验：
        UserInfo userInfo = await Login(ck);

        DailyTaskInfo dailyTaskInfo = await GetDailyTaskStatus(ck);
        await WatchAndShareVideo(dailyTaskInfo, ck);

        await AddCoins(userInfo, ck);

        //签到：
        await MangaSign(ck);
        await MangaRead(ck);
        await ExchangeSilver2Coin(ck);

        //领福利：
        await ReceiveVipPrivilege(userInfo, ck);
        await ReceiveMangaVipReward(userInfo, ck);

        await Charge(userInfo, ck);
    }

    [TaskInterceptor("Set Cookie")]
    private async Task SetCookiesAsync(BiliCookie biliCookie, CancellationToken cancellationToken)
    {
        //判断cookie是否完整
        if (biliCookie.Buvid.IsNotNullOrEmpty())
        {
            logger.LogInformation("Cookie完整，不需要Set Cookie");
            return;
        }

        //Set
        logger.LogInformation("开始Set Cookie");
        var ck = await loginDomainService.SetCookieAsync(biliCookie, cancellationToken);

        //持久化
        logger.LogInformation("持久化Cookie");
        await SaveCookieAsync(ck, cancellationToken);
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <returns></returns>
    [TaskInterceptor("登录")]
    private async Task<UserInfo> Login(BiliCookie ck)
    {
        UserInfo userInfo = await accountDomainService.LoginByCookie(ck);
        if (userInfo == null)
            throw new Exception("登录失败，请检查Cookie"); //终止流程

        _expDic.TryGetValue("每日登录", out int exp);
        logger.LogInformation("登录成功，经验+{exp} √", exp);

        return userInfo;
    }

    /// <summary>
    /// 获取任务完成情况
    /// </summary>
    /// <returns></returns>
    [TaskInterceptor(rethrowWhenException: false)]
    private async Task<DailyTaskInfo> GetDailyTaskStatus(BiliCookie ck)
    {
        return await accountDomainService.GetDailyTaskStatus(ck);
    }

    /// <summary>
    /// 观看、分享视频
    /// </summary>
    [TaskInterceptor("观看、分享视频", rethrowWhenException: false)]
    private async Task WatchAndShareVideo(DailyTaskInfo dailyTaskInfo, BiliCookie ck)
    {
        if (!_dailyTaskOptions.IsWatchVideo && !_dailyTaskOptions.IsShareVideo)
        {
            logger.LogInformation("已配置为关闭，跳过任务");
            return;
        }

        await videoDomainService.WatchAndShareVideo(dailyTaskInfo, ck);
    }

    /// <summary>
    /// 投币任务
    /// </summary>
    [TaskInterceptor("投币", rethrowWhenException: false)]
    private async Task AddCoins(UserInfo userInfo, BiliCookie ck)
    {
        if (_dailyTaskOptions.SaveCoinsWhenLv6 && userInfo.Level_info.Current_level >= 6)
        {
            logger.LogInformation("已经为LV6大佬，开始白嫖");
            return;
        }

        if (_dailyTaskOptions.IsDonateCoinForArticle)
        {
            logger.LogInformation("专栏投币已开启");

            if (!await articleDomainService.AddCoinForArticles(ck))
            {
                logger.LogInformation("专栏投币结束，转入视频投币");
                await donateCoinDomainService.AddCoinsForVideos(ck);
            }
        }
        else
        {
            await donateCoinDomainService.AddCoinsForVideos(ck);
        }
    }

    /// <summary>
    /// 直播中心的银瓜子兑换硬币
    /// </summary>
    [TaskInterceptor("银瓜子兑换硬币", rethrowWhenException: false)]
    private async Task ExchangeSilver2Coin(BiliCookie ck)
    {
        var success = await liveDomainService.ExchangeSilver2Coin(ck);
        if (!success)
            return;

        //如果兑换成功，则打印硬币余额
        var coinBalance = coinDomainService.GetCoinBalance(ck);
        logger.LogInformation("【硬币余额】 {coin}", coinBalance);
    }

    /// <summary>
    /// 每月领取大会员福利
    /// </summary>
    [TaskInterceptor("领取大会员福利", rethrowWhenException: false)]
    private async Task ReceiveVipPrivilege(UserInfo userInfo, BiliCookie ck)
    {
        var suc = await vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo, ck);

        //如果领取成功，需要刷新账户信息（比如B币余额）
        if (suc)
        {
            try
            {
                userInfo = await accountDomainService.LoginByCookie(ck);
            }
            catch (Exception ex)
            {
                logger.LogError("领取福利成功，但之后刷新用户信息时异常，信息：{msg}", ex.Message);
            }
        }
    }

    /// <summary>
    /// 每月为自己充电
    /// </summary>
    [TaskInterceptor("B币券充电", rethrowWhenException: false)]
    private async Task Charge(UserInfo userInfo, BiliCookie ck)
    {
        await chargeDomainService.Charge(userInfo, ck);
    }

    /// <summary>
    /// 漫画签到
    /// </summary>
    [TaskInterceptor("漫画签到", rethrowWhenException: false)]
    private async Task MangaSign(BiliCookie ck)
    {
        await mangaDomainService.MangaSign(ck);
    }

    /// <summary>
    /// 漫画阅读
    /// </summary>
    [TaskInterceptor("漫画阅读", rethrowWhenException: false)]
    private async Task MangaRead(BiliCookie ck)
    {
        await mangaDomainService.MangaRead(ck);
    }

    /// <summary>
    /// 每月获取大会员漫画权益
    /// </summary>
    [TaskInterceptor("领取大会员漫画权益", rethrowWhenException: false)]
    private async Task ReceiveMangaVipReward(UserInfo userInfo, BiliCookie ck)
    {
        await mangaDomainService.ReceiveMangaVipReward(1, userInfo, ck);
    }

    private async Task SaveCookieAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
    {
        var platformType = configuration.GetSection("PlatformType").Get<PlatformType>();
        logger.LogInformation("当前运行平台：{platform}", platformType);

        //更新cookie到青龙env
        if (platformType == PlatformType.QingLong)
        {
            await loginDomainService.SaveCookieToQinLongAsync(ckInfo, cancellationToken);
            return;
        }

        //更新cookie到json
        await loginDomainService.SaveCookieToJsonFileAsync(ckInfo, cancellationToken);
    }
}
