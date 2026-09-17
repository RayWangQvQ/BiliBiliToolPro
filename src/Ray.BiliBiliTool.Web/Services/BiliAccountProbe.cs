using System.Collections.Concurrent;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record BiliAccountProbeResult(
    bool Success,
    string? UserName,
    int? Level,
    string? Message,
    DateTimeOffset CheckedAt
);

public interface IBiliAccountProbe
{
    Task<BiliAccountProbeResult> ProbeAsync(
        long userId,
        bool force = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>并发探测多个账号（不存在的账号直接返回失败结果）</summary>
    Task<Dictionary<long, BiliAccountProbeResult>> ProbeManyAsync(
        IEnumerable<long> userIds,
        bool force = false,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// 账号探测：调用 B 站 nav 接口拿昵称/等级/是否登录。
/// 结果缓存一段时间，避免每次打开页面都打 B 站；账号页与今日任务页共用。
/// </summary>
public class BiliAccountProbe(
    CookieStrFactory<BiliCookie> cookieStrFactory,
    IAccountDomainService accountDomainService,
    ILogger<BiliAccountProbe> logger
) : IBiliAccountProbe
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(60);

    private static readonly ConcurrentDictionary<long, BiliAccountProbeResult> Cache = new();

    /// <summary>单个账号探测的超时时间，避免 B 站卡住时页面一直转圈</summary>
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(15);

    public async Task<BiliAccountProbeResult> ProbeAsync(
        long userId,
        bool force = false,
        CancellationToken cancellationToken = default
    )
    {
        if (!force && Cache.TryGetValue(userId, out var cached))
        {
            if (DateTimeOffset.UtcNow - cached.CheckedAt < CacheLifetime)
            {
                return cached;
            }
        }

        var ck = FindCookie(userId);
        if (ck is null)
        {
            return new BiliAccountProbeResult(
                false,
                null,
                null,
                "未找到该账号的 Cookie",
                DateTimeOffset.UtcNow
            );
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        BiliAccountProbeResult result;
        try
        {
            var userInfo = await accountDomainService.LoginByCookie(ck);
            if (userInfo is null)
            {
                result = new BiliAccountProbeResult(
                    false,
                    null,
                    null,
                    "B站返回空结果，可能是 Cookie 已失效",
                    DateTimeOffset.UtcNow
                );
            }
            else
            {
                result = new BiliAccountProbeResult(
                    true,
                    userInfo.Uname,
                    userInfo.Level_info?.Current_level,
                    null,
                    DateTimeOffset.UtcNow
                );
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            result = new BiliAccountProbeResult(
                false,
                null,
                null,
                "检测超时（B站响应过慢）",
                DateTimeOffset.UtcNow
            );
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "探测账号 {uid} 失败", userId);
            result = new BiliAccountProbeResult(
                false,
                null,
                null,
                $"检测失败：{ex.Message}",
                DateTimeOffset.UtcNow
            );
        }

        Cache[userId] = result;
        return result;
    }

    public async Task<Dictionary<long, BiliAccountProbeResult>> ProbeManyAsync(
        IEnumerable<long> userIds,
        bool force = false,
        CancellationToken cancellationToken = default
    )
    {
        var ids = userIds.Distinct().ToList();
        var tasks = ids.Select(async id =>
            (Id: id, Result: await ProbeAsync(id, force, cancellationToken))
        );
        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(x => x.Id, x => x.Result);
    }

    private BiliCookie? FindCookie(long userId)
    {
        for (int i = 0; i < cookieStrFactory.Count; i++)
        {
            var ck = cookieStrFactory.GetCookie(i);
            if (ck.UserId == userId.ToString())
            {
                return ck;
            }
        }

        return null;
    }
}
