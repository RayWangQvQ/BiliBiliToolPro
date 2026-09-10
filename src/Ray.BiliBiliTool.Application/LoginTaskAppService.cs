using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Diagnostics;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application;

/// <summary>
/// Application service for automating BiliBili QR code login flow.
/// Orchestrates the three-step login process: QR code generation → cookie validation → platform-aware persistence.
/// </summary>
/// <remarks>
/// This service is designed for automation task scenarios (triggered by LoginJob)
/// and delegates all domain logic to ILoginDomainService.
/// The TaskInterceptor attributes enable telemetry and logging for each workflow step.
/// </remarks>
public class LoginTaskAppService(
    IConfiguration configuration,
    ILogger<LoginTaskAppService> logger,
    ILoginDomainService loginDomainService
) : AppService, ILoginTaskAppService
{
    /// <summary>
    /// Executes the complete automated login workflow using QR code authentication.
    /// </summary>
    /// <remarks>
    /// The workflow consists of three sequential steps:
    /// 1. QR Code Login: Generate QR code and wait for user scan
    /// 2. Set Cookies: Validate scanned credentials and enrich cookie context
    /// 3. Save Cookie: Persist credentials to platform-specific storage (QingLong or JSON file)
    ///
    /// The entire flow is wrapped in TaskFlowDiagnosticScope for before/after comparison during refactoring.
    /// </remarks>
    [TaskInterceptor("扫码登录", TaskLevel.One)]
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        await TaskFlowDiagnosticScope.ExecuteAsync(
            logger,
            "LoginTask",
            async () =>
            {
                // Step 1: Generate QR code and wait for user to scan via mobile app
                //扫码登录
                var cookieInfo = await QrCodeLoginAsync(cancellationToken);
                if (cookieInfo == null)
                    return;

                // Step 2: Validate and enrich the cookie with additional context
                //set cookie
                cookieInfo = await SetCookiesAsync(cookieInfo, cancellationToken);

                // Step 3: Persist cookie to platform-specific storage
                //持久化cookie
                await SaveCookieAsync(cookieInfo, cancellationToken);
            }
        );
    }

    /// <summary>
    /// Step 1: Initiates QR code-based authentication flow.
    /// Delegates to domain service to generate QR code and wait for user scan.
    /// </summary>
    /// <returns>Initial cookie information from QR code scan, or null if login failed</returns>
    [TaskInterceptor("获取二维码")]
    private async Task<BiliCookie> QrCodeLoginAsync(CancellationToken cancellationToken)
    {
        var biliCookie = await loginDomainService.LoginByQrCodeAsync(cancellationToken);
        return biliCookie;
    }

    /// <summary>
    /// Step 2: Validates and enriches cookie context.
    /// Delegates to domain service to perform server-side validation and add additional cookie fields.
    /// </summary>
    /// <param name="biliCookie">Initial cookie from QR code scan</param>
    /// <returns>Validated and enriched cookie ready for persistence</returns>
    [TaskInterceptor("Set Cookie")]
    private async Task<BiliCookie> SetCookiesAsync(
        BiliCookie biliCookie,
        CancellationToken cancellationToken
    )
    {
        var ck = await loginDomainService.SetCookieAsync(biliCookie, cancellationToken);
        return ck;
    }

    /// <summary>
    /// Step 3: Persists cookie to platform-specific storage.
    /// Routes to QingLong environment variables (for automation platforms)
    /// or JSON file (for local/standalone environments).
    /// </summary>
    /// <param name="ckInfo">Validated cookie ready for persistence</param>
    [TaskInterceptor("持久化Cookie")]
    private async Task SaveCookieAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
    {
        var platformType = configuration.GetSection("PlatformType").Get<PlatformType>();
        logger.LogInformation("当前运行平台：{platform}", platformType);

        // Platform-aware persistence routing:
        // QingLong: Automation platform - save to environment variables for scheduled tasks
        // Otherwise: Local/standalone - save to JSON file for manual execution
        //更新cookie到青龙env
        if (platformType == PlatformType.QingLong)
        {
            await loginDomainService.SaveCookieToQinLongAsync(ckInfo, cancellationToken);
            return;
        }

        //更新cookie到白虎env
        if (platformType == PlatformType.Baihu)
        {
            await loginDomainService.SaveCookieToBaihuAsync(ckInfo, cancellationToken);
            return;
        }

        //更新cookie到json
        await loginDomainService.SaveCookieToJsonFileAsync(ckInfo, cancellationToken);
    }
}
