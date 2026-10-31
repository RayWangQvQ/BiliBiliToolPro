using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Application;

public class WatchLaterCleanupAppService(
    ILogger<WatchLaterCleanupAppService> logger,
    IOptionsMonitor<WatchLaterCleanupTaskOptions> watchLaterCleanupTaskOptions,
    IAccountDomainService accountDomainService,
    ILoginDomainService loginDomainService,
    IWatchLaterDomainService watchLaterDomainService,
    IConfiguration configuration,
    CookieStrFactory<BiliCookie> cookieStrFactory,
    ICookieTaskGuard cookieTaskGuard
)
    : BaseMultiAccountsAppService(
        logger,
        cookieStrFactory,
        loginDomainService,
        configuration,
        cookieTaskGuard
    ),
        IWatchLaterCleanupAppService
{
    [TaskInterceptor("稍后再看清理", TaskLevel.One)]
    protected override async Task DoTaskAccountAsync(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    )
    {
        var options = watchLaterCleanupTaskOptions.CurrentValue;

        if (!options.IsEnable)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return;
        }

        await SetCookiesAsync(ck, cancellationToken);
        await accountDomainService.LoginByCookie(ck);

        if (options.OnlyViewed)
        {
            await watchLaterDomainService.CleanViewedAsync(
                ck,
                pageSize: options.PageSize,
                intervalMs: options.DeleteIntervalMs,
                alsoDeleteInvalid: options.DeleteInvalid,
                viewedThreshold: options.ViewedThreshold,
                useHistoryCrossCheck: options.UseHistoryCrossCheck,
                historyMaxPages: options.HistoryMaxPages,
                backupBeforeClean: options.BackupBeforeClean,
                backupDirectory: options.BackupDirectory,
                backupRetentionDays: options.BackupRetentionDays,
                cancellationToken: cancellationToken
            );
            return;
        }

        await watchLaterDomainService.CleanAllAsync(
            ck,
            pageSize: options.PageSize,
            intervalMs: options.DeleteIntervalMs,
            backupBeforeClean: options.BackupBeforeClean,
            backupDirectory: options.BackupDirectory,
            backupRetentionDays: options.BackupRetentionDays,
            cancellationToken: cancellationToken
        );
    }
}
