using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record LiveMedalAccount(int Index, string Label);

public sealed record LiveMedalTaskProgress(
    string Action,
    string Title,
    string Progress,
    bool Done,
    double? Percent
)
{
    public Ray.BiliBiliTool.DomainService.LiveFansMedalPlan? GetPlan(
        bool? lighted,
        bool savingsFull
    )
    {
        if (lighted is null || Action is not ("like" or "sendDanmu" or "watchLive"))
            return null;
        try
        {
            return Ray.BiliBiliTool.DomainService.LiveFansMedalTaskPlanner.Plan(
                new()
                {
                    Is_lighted = lighted.Value,
                    Reach_free_intimacy_limit = savingsFull,
                    Task_info =
                    [
                        new()
                        {
                            Jump_type = Action,
                            Title = Title,
                            Sub_title = Progress,
                            Is_done = Done,
                        },
                    ],
                },
                Action
            );
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public string? RemainingText(bool? lighted, bool savingsFull)
    {
        var plan = GetPlan(lighted, savingsFull);
        return plan is null ? null
            : Action == "watchLive" ? $"还需 {plan.Remaining / 60m:0.#} 分钟"
            : $"还需 {plan.Remaining} {(Action == "like" ? "次点赞" : "条弹幕")}";
    }

    public static LiveMedalTaskProgress From(FansMedalTaskInfo task)
    {
        var match = Regex.Match(task.Sub_title, @"(\d+(?:\.\d+)?)\s*/\s*(\d+(?:\.\d+)?)");
        double? percent = null;
        if (
            match.Success
            && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var current)
            && double.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var total)
            && total > 0
        )
            percent = Math.Clamp(current / total * 100, 0, 100);
        return new(
            task.Jump_type,
            task.Title,
            task.Sub_title,
            task.Is_done,
            task.Is_done ? 100 : percent
        );
    }
}

public sealed record LiveMedalCard(
    long AnchorId,
    string AnchorName,
    string MedalName,
    int Level,
    bool Live,
    bool? Lighted,
    bool SavingsFull,
    IReadOnlyList<LiveMedalTaskProgress> Tasks,
    string? Error,
    bool CanInteract = true,
    long RoomId = 0,
    DateTimeOffset? ProgressUpdatedAt = null
);

public sealed record LiveMedalSnapshot(
    IReadOnlyList<LiveMedalCard> Medals,
    DateTimeOffset UpdatedAt,
    string? Error = null,
    [property: System.Text.Json.Serialization.JsonIgnore] long Revision = 0
);

public interface ILiveMedalDashboardService
{
    TimeSpan AutoRefreshInterval => TimeSpan.FromMinutes(1);
    IDisposable? Subscribe(int accountIndex, Action<LiveMedalSnapshot> changed) => null;
    IReadOnlyList<LiveMedalAccount> GetAccounts();
    Task<LiveMedalSnapshot?> GetCachedAsync(int accountIndex, CancellationToken token = default);
    Task<LiveMedalSnapshot> GetAsync(
        int accountIndex,
        bool refresh = false,
        CancellationToken token = default
    );
}

public sealed class LiveMedalDashboardService(
    CookieStrFactory<BiliCookie> cookies,
    ILiveApi api,
    IMemoryCache cache,
    ILogger<LiveMedalDashboardService> logger,
    ILiveMedalSnapshotStore? snapshots = null,
    LiveMedalProgressUpdates? updates = null
) : ILiveMedalDashboardService
{
    public IDisposable? Subscribe(int accountIndex, Action<LiveMedalSnapshot> changed)
    {
        var key = LiveMedalProgressUpdates.AccountKey(cookies.GetCookie(accountIndex));
        return updates?.Subscribe(
            key,
            snapshot =>
            {
                if (
                    accountIndex < cookies.Count
                    && key == LiveMedalProgressUpdates.AccountKey(cookies.GetCookie(accountIndex))
                )
                    changed(snapshot);
            }
        );
    }

    public IReadOnlyList<LiveMedalAccount> GetAccounts() =>
        Enumerable
            .Range(0, cookies.Count)
            .Select(index => new LiveMedalAccount(index, $"账号 {index + 1}"))
            .ToList();

    private string AccountKey(int index) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(cookies.GetCookie(index).ToString()))
        );

    public async Task<LiveMedalSnapshot?> GetCachedAsync(
        int accountIndex,
        CancellationToken token = default
    )
    {
        token.ThrowIfCancellationRequested();
        if (updates?.Get(cookies.GetCookie(accountIndex)) is { } live)
            return live;
        var fingerprint = AccountKey(accountIndex);
        var key = $"live-medals:last:{fingerprint}";
        if (cache.TryGetValue<LiveMedalSnapshot>(key, out var cached))
            return cached;
        var stored = snapshots is null ? null : await snapshots.ReadAsync(fingerprint, token);
        if (stored is not null)
            cache.Set(key, stored, stored.UpdatedAt.AddDays(7));
        return stored;
    }

    public async Task<LiveMedalSnapshot> GetAsync(
        int accountIndex,
        bool refresh = false,
        CancellationToken token = default
    )
    {
        // Cache follows both the login credentials and the Bilibili calendar day.
        var cookie = cookies.GetCookie(accountIndex);
        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(cookie.ToString()))
        );
        var key =
            $"live-medals:{fingerprint}:{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)):yyyyMMdd}";
        if (!refresh && cache.TryGetValue<LiveMedalSnapshot>(key, out var cached))
            return updates?.Get(cookie) ?? cached!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            var medals = new Dictionary<long, FansMedalPanelItem>();
            for (var page = 1; ; page++)
            {
                var data = Require(
                    await api.GetFansMedalPanel(page, cookie.ToString(), timeout.Token)
                );
                foreach (var medal in data.Special_list.Concat(data.List))
                    if (medal.Medal.Target_id > 0)
                        medals.TryAdd(medal.Medal.Target_id, medal);
                if (!data.Page_info.Has_more && page >= data.Page_info.Total_page)
                    break;
                if (page >= 1000)
                    throw new InvalidOperationException("粉丝牌列表暂未加载完成，请刷新");
                await Task.Delay(200, timeout.Token);
            }

            using var slots = new SemaphoreSlim(2);
            async Task<LiveMedalCard> ReadAsync(FansMedalPanelItem medal)
            {
                var acquired = false;
                try
                {
                    await slots.WaitAsync(timeout.Token);
                    acquired = true;
                    await Task.Delay(150, timeout.Token);
                    var observedAt = DateTimeOffset.UtcNow;
                    var tasks = Require(
                        await api.GetActivatedMedalInfo(
                            medal.Medal.Target_id,
                            cookie.BiliJct,
                            cookie.ToString(),
                            timeout.Token
                        )
                    );
                    return Card(medal, tasks, null) with { ProgressUpdatedAt = observedAt };
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Medal progress read failed: {ErrorType}", ex.GetType().Name);
                    return Card(medal, null, SafeMessage(ex));
                }
                finally
                {
                    if (acquired)
                        slots.Release();
                }
            }
            var cards = await Task.WhenAll(medals.Values.Select(ReadAsync));
            var result = new LiveMedalSnapshot(
                cards
                    .OrderByDescending(card => card.Level)
                    .ThenBy(card => card.AnchorName)
                    .ToList(),
                DateTimeOffset.UtcNow
            );
            result = updates?.Publish(cookie, result) ?? result;
            if (cards.All(card => card.Error is null))
            {
                cache.Set(key, result, TimeSpan.FromSeconds(60));
            }
            // Keep the panel available even if an individual progress read needs another refresh.
            cache.Set($"live-medals:last:{fingerprint}", result, TimeSpan.FromDays(7));
            if (snapshots is not null)
                await snapshots.WriteAsync(fingerprint, result, token);
            return result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug("Medal panel read failed: {ErrorType}", ex.GetType().Name);
            return new([], DateTimeOffset.UtcNow, SafeMessage(ex));
        }
    }

    private static LiveMedalCard Card(
        FansMedalPanelItem medal,
        ActivatedMedalResponse? tasks,
        string? error
    ) =>
        new(
            medal.Medal.Target_id,
            string.IsNullOrWhiteSpace(medal.Anchor_info.Nick_name)
                ? $"主播 {medal.Medal.Target_id}"
                : medal.Anchor_info.Nick_name,
            medal.Medal.Medal_name,
            tasks?.Level > 0 ? tasks.Level : medal.Medal.Level,
            medal.Room_info.Living_status == 1,
            tasks?.Is_lighted,
            tasks?.Reach_free_intimacy_limit ?? false,
            tasks?.Task_info.Select(LiveMedalTaskProgress.From).ToList() ?? [],
            error,
            medal.Room_info.Room_id > 0,
            medal.Room_info.Room_id,
            DateTimeOffset.UtcNow
        );

    private static T Require<T>(BiliApiResponse<T> response)
        where T : class
    {
        if (response.Code == -101)
            throw new LoginRequiredException();
        if (response.Code != 0 || response.Data is null)
            throw new InvalidOperationException();
        return response.Data;
    }

    private static string SafeMessage(Exception error) =>
        error is LoginRequiredException ? "请重新登录账号后刷新" : "任务进度暂未加载，请刷新";

    private sealed class LoginRequiredException : Exception { }
}
