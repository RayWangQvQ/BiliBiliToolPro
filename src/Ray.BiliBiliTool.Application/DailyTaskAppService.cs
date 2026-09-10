using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Diagnostics;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

/// <summary>
/// Application service for automating BiliBili daily task execution flow.
/// Orchestrates the six-step daily task workflow: cookie setup → login → task status → watch/share → coin donation → VIP privilege claiming.
/// </summary>
/// <remarks>
/// This service is designed for automation task scenarios (triggered by DailyJob)
/// and delegates domain operations to multiple specialized domain services.
/// The service inherits from BaseMultiAccountsAppService to provide multi-account iteration
/// and error resilience (continues execution even when one account fails).
///
/// The workflow consists of six sequential steps:
/// 1. SetCookiesAsync: Validates cookie completeness and enriches context if needed
/// 2. Login: Authenticates user and retrieves account information
/// 3. GetDailyTaskStatus: Fetches current task completion status
/// 4. WatchAndShareVideo: Completes video watch and share requirements (configuration-driven)
/// 5. AddCoins: Donates coins to articles or videos based on configuration
/// 6. ReceiveVipPrivilege: Claims monthly VIP membership benefits
///
/// The entire flow is wrapped in TaskFlowDiagnosticScope for operational observability.
/// TaskInterceptor attributes enable telemetry and logging for each workflow step.
/// Configuration flags (DailyTaskOptions) control conditional execution of specific steps.
/// </remarks>
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
)
    : BaseMultiAccountsAppService(logger, cookieStrFactory, loginDomainService, configuration),
        IDailyTaskAppService
{
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly Dictionary<string, int> _expDic = Config.Constants.ExpDic;

    /// <summary>
    /// Executes the complete daily task workflow for a single account.
    /// This method is invoked by the multi-account base class for each cookie in the configured accounts list.
    /// </summary>
    /// <remarks>
    /// The workflow is wrapped in TaskFlowDiagnosticScope with "DailyTask" label for before/after comparison during refactoring.
    /// Configuration flag IsEnable provides an early exit if daily tasks are disabled.
    /// The six-step sequence orchestrates multiple domain services while maintaining clear separation of concerns.
    /// </remarks>
    /// <param name="ck">BiliBili cookie containing authentication credentials for the current account</param>
    /// <param name="cancellationToken">Cancellation token for async operation control</param>
    [TaskInterceptor("每日任务", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "DailyTask",
            async () =>
            {
                // Configuration-driven early exit: skip if daily tasks are disabled
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
        );
    }

    /// <summary>
    /// Step 2: Authenticates user and retrieves account information.
    /// Delegates to account domain service for cookie-based authentication.
    /// Logs experience points earned from daily login bonus.
    /// </summary>
    /// <param name="ck">BiliBili cookie containing authentication credentials</param>
    /// <returns>User account information including level, experience, and VIP status</returns>
    [TaskInterceptor("登录")]
    private async Task<UserInfo> Login(BiliCookie ck)
    {
        UserInfo userInfo = await accountDomainService.LoginByCookie(ck);

        _expDic.TryGetValue("每日登录", out int exp);
        logger.LogInformation("登录成功，经验+{exp} √", exp);

        return userInfo;
    }

    /// <summary>
    /// Step 3: Retrieves current daily task completion status.
    /// Delegates to account domain service to fetch task progress from BiliBili API.
    /// Exception handling is disabled (rethrowWhenException: false) to allow workflow to continue even if status check fails.
    /// </summary>
    /// <param name="ck">BiliBili cookie containing authentication credentials</param>
    /// <returns>Daily task status including watch, share, and coin donation completion state</returns>
    [TaskInterceptor(rethrowWhenException: false)]
    private async Task<DailyTaskInfo> GetDailyTaskStatus(BiliCookie ck)
    {
        return await accountDomainService.GetDailyTaskStatus(ck);
    }

    /// <summary>
    /// Step 4: Completes video watch and share requirements.
    /// Configuration flags IsWatchVideo and IsShareVideo control whether this step executes.
    /// Delegates to video domain service for actual watch and share operations.
    /// Exception handling is disabled to allow workflow to continue even if watch/share fails.
    /// </summary>
    /// <param name="dailyTaskInfo">Current task status used to determine which operations are needed</param>
    /// <param name="ck">BiliBili cookie containing authentication credentials</param>
    [TaskInterceptor("观看、分享视频", rethrowWhenException: false)]
    private async Task WatchAndShareVideo(DailyTaskInfo dailyTaskInfo, BiliCookie ck)
    {
        // Configuration-driven conditional: skip if both watch and share are disabled
        if (!_dailyTaskOptions.IsWatchVideo && !_dailyTaskOptions.IsShareVideo)
        {
            logger.LogInformation("已配置为关闭，跳过任务");
            return;
        }

        await videoDomainService.WatchAndShareVideo(dailyTaskInfo, ck);
    }

    /// <summary>
    /// Step 5: Donates coins to articles or videos based on configuration.
    /// LV6 optimization: Users at max level (6) can skip coin donation to conserve coins (controlled by SaveCoinsWhenLv6 flag).
    /// Article-first routing: If IsDonateCoinForArticle is enabled, attempts article donation first, falling back to video donation if unsuccessful.
    /// Otherwise routes directly to video coin donation.
    /// Exception handling is disabled to allow workflow to continue even if coin donation fails.
    /// </summary>
    /// <param name="userInfo">User account information including current level</param>
    /// <param name="ck">BiliBili cookie containing authentication credentials</param>
    [TaskInterceptor("投币", rethrowWhenException: false)]
    private async Task AddCoins(UserInfo userInfo, BiliCookie ck)
    {
        // LV6 optimization: skip coin donation for max-level users to conserve coins
        if (_dailyTaskOptions.SaveCoinsWhenLv6 && userInfo.Level_info?.Current_level >= 6)
        {
            logger.LogInformation("已经为LV6大佬，开始白嫖");
            return;
        }

        // Configuration-driven routing: article-first vs video-only donation
        if (_dailyTaskOptions.IsDonateCoinForArticle)
        {
            logger.LogInformation("专栏投币已开启");

            // Fallback pattern: article donation returns false if no suitable articles found, triggering video donation
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
    /// Step 6: Claims monthly VIP membership benefits (B coins, coupons, etc.).
    /// Delegates to VIP privilege domain service for benefit claiming operations.
    /// If claiming succeeds, refreshes account information to reflect updated B coin balance.
    /// Exception handling is disabled for the main operation; refresh failures are logged but don't block workflow.
    /// </summary>
    /// <param name="userInfo">User account information including VIP status</param>
    /// <param name="ck">BiliBili cookie containing authentication credentials</param>
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
}
