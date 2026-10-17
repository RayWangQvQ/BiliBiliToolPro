using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Application.Contracts.Notifications;

namespace Ray.BiliBiliTool.Application;

public sealed class CookieTaskGuard(
    INavApi navApi,
    ICookieCheckStateStore stateStore,
    INotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<CookieTaskGuard> logger,
    IConfiguration configuration
) : ICookieTaskGuard
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly TimeSpan NotificationRetryDelay = TimeSpan.FromMinutes(15);

    public Task EnsureValidAsync(
        string userId,
        string cookie,
        CancellationToken cancellationToken = default
    ) =>
        configuration.GetValue("CookieCheck:AutoCheckEnabled", true)
            ? CheckAsync(userId, cookie, false, cancellationToken)
            : Task.CompletedTask;

    public Task CheckNowAsync(
        string userId,
        string cookie,
        CancellationToken cancellationToken = default
    ) => CheckAsync(userId, cookie, true, cancellationToken);

    private async Task CheckAsync(
        string userId,
        string cookie,
        bool force,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(userId) || !userId.All(char.IsAsciiDigit))
            throw new InvalidOperationException("账号配置缺少有效 UID，请重新登录");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookie)));
            var state = await stateStore.ReadAsync(userId, cancellationToken);

            if (
                force
                || state is null
                || state.CheckedDate != today
                || state.Fingerprint != fingerprint
            )
            {
                bool valid;
                try
                {
                    var response = await navApi.GetNavAsync(cookie, cancellationToken);
                    if (
                        response.Code == -101
                        || (response.Code == 0 && response.Data?.IsLogin == false)
                    )
                        valid = false;
                    else if (response.Code == 0 && response.Data?.IsLogin == true)
                        valid = true;
                    else
                        throw new InvalidOperationException("Unconfirmed login state");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // A transport or business error is not evidence of an expired session.
                    // Timeout is governed by HttpClient.Timeout (30s), not a local CancelAfter.
                    logger.LogWarning(
                        ex,
                        "Cookie check failed for user {UserId}: {Message}",
                        userId,
                        ex.Message
                    );
                    throw new InvalidOperationException(
                        "登录态检查暂时失败，本次活动已跳过，稍后自动重试"
                    );
                }

                state = new CookieCheckState(
                    fingerprint,
                    today,
                    valid,
                    state?.NotifiedDate,
                    state?.NotificationAttempt
                );
                await stateStore.WriteAsync(userId, state, cancellationToken);
            }

            if (state.IsValid)
                return;

            var now = timeProvider.GetUtcNow();
            if (
                state.NotifiedDate != today
                && (
                    state.NotificationAttempt is null
                    || now - state.NotificationAttempt >= NotificationRetryDelay
                )
            )
            {
                state = state with { NotificationAttempt = now };
                await stateStore.WriteAsync(userId, state, cancellationToken);
                var maskedAccount = "***" + userId[^Math.Min(4, userId.Length)..];
                await notificationService.SendAsync(
                    new NotificationMessage(
                        "BiliBili Cookie 已过期",
                        $"账号 {maskedAccount} 的登录凭证已失效，本次活动已自动跳过。\n\n请打开 BiliBiliToolPro 面板，在「账号管理」中重新扫码登录。\n\n同一账号每天最多发送一次过期提醒。",
                        NotificationLevel.Warning
                    ),
                    userId
                );
                state = state with { NotifiedDate = today };
                await stateStore.WriteAsync(userId, state, cancellationToken);
            }

            throw new InvalidOperationException(
                "Cookie 已过期，本次活动已跳过，请在账号管理中重新登录"
            );
        }
        finally
        {
            _gate.Release();
        }
    }
}
