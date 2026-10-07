using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts.Cookies;

namespace Ray.BiliBiliTool.Application;

public sealed class CookieTaskGuard(
    INavApi navApi,
    ICookieCheckStateStore stateStore,
    ICookieExpiryNotifier notifier,
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
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken
                    );
                    timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    var response = await navApi.GetNavAsync(cookie, timeout.Token);
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
                catch (Exception)
                {
                    // A transport or business error is not evidence of an expired session.
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
                try
                {
                    var maskedAccount = "***" + userId[^Math.Min(4, userId.Length)..];
                    if (await notifier.SendAsync(maskedAccount, cancellationToken))
                    {
                        state = state with { NotifiedDate = today };
                        await stateStore.WriteAsync(userId, state, cancellationToken);
                    }
                    else
                        logger.LogWarning(
                            "Cookie 过期提醒未发送，请检查 Server 酱通知配置。稍后自动重试"
                        );
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Provider exceptions can contain the SendKey in the request URL.
                    logger.LogWarning("Cookie 过期提醒发送失败，稍后自动重试");
                }
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
