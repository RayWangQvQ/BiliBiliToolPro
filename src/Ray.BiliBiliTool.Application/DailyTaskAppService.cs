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
    IVipPrivilegeDomainService vipPrivilegeDomainService,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
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
        if (!_dailyTaskOptions.IsEnable)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return;
        }

        await SetCookiesAsync(ck, cancellationToken);

        //每日任务赚经验：
        UserInfo userInfo = await Login(ck);

        DailyTaskInfo dailyTaskInfo = await GetDailyTaskStatus(ck);
        await WatchAndShareVideo(dailyTaskInfo, ck);

        await AddCoins(userInfo, ck);

        await ReceiveVipPrivilege(userInfo, ck);
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
        if (_dailyTaskOptions.SaveCoinsWhenLv6 && userInfo.Level_info?.Current_level >= 6)
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
                await accountDomainService.LoginByCookie(ck);
            }
            catch (Exception ex)
            {
                logger.LogError("领取福利成功，但之后刷新用户信息时异常，信息：{msg}", ex.Message);
            }
        }
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
