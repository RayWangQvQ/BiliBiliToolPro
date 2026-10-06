using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record LiveMedalMonitorAccount(
    int Index,
    long UserId,
    string CredentialVersion,
    LiveMedalSnapshot Snapshot
);

public sealed record LiveMedalMonitorTarget(
    int Index,
    long UserId,
    string CredentialVersion,
    long AnchorId,
    long RoomId,
    string Action
);

public interface ILiveMedalMonitorSource
{
    Task<IReadOnlyList<LiveMedalMonitorAccount>> ReadAsync(
        LiveFansMedalTaskOptions options,
        CancellationToken token
    );
    Task ExecuteAsync(LiveMedalMonitorTarget target, CancellationToken token);
}

public sealed class LiveMedalMonitorSource(
    IServiceScopeFactory scopes,
    ILogger<LiveMedalMonitorSource> logger
) : ILiveMedalMonitorSource
{
    public static string CredentialVersion(BiliCookie cookie) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookie.ToString())));

    public async Task<IReadOnlyList<LiveMedalMonitorAccount>> ReadAsync(
        LiveFansMedalTaskOptions options,
        CancellationToken token
    )
    {
        await using var scope = scopes.CreateAsyncScope();
        var cookies = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
        var reader = scope.ServiceProvider.GetRequiredService<ILiveMedalDashboardService>();
        var guard = scope.ServiceProvider.GetRequiredService<ICookieTaskGuard>();
        var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
        var excluded = options.GetExcludedAnchorIds();
        var selected = options.GetIncludedAnchorIds();
        var result = new List<LiveMedalMonitorAccount>();
        for (var index = 0; index < cookies.Count; index++)
        {
            try
            {
                var cookie = cookies.GetCookie(index);
                if (!long.TryParse(cookie.UserId, out var uid))
                    continue;
                await guard.EnsureValidAsync(cookie.UserId, cookie.ToString(), token);
                var snapshot = await reader.GetAsync(index, refresh: true, token: token);
                var cards = new List<LiveMedalCard>();
                foreach (var medal in snapshot.Medals)
                {
                    token.ThrowIfCancellationRequested();
                    if (
                        medal.RoomId <= 0
                        || excluded.Contains(medal.AnchorId)
                        || (options.OnlySelectedAnchors && !selected.Contains(medal.AnchorId))
                        || medal.Tasks.All(task => task.Done)
                    )
                    {
                        cards.Add(medal);
                        continue;
                    }
                    try
                    {
                        var room = await api.GetLiveRoomInfo(medal.RoomId).WaitAsync(token);
                        cards.Add(
                            room.Code == 0 && room.Data is not null
                                ? medal with
                                {
                                    Live = room.Data.Live_Status == 1,
                                }
                                : medal with
                                {
                                    Error = "直播状态暂未获取",
                                }
                        );
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        cards.Add(medal with { Error = "直播状态暂未获取" });
                    }
                }
                result.Add(
                    new(index, uid, CredentialVersion(cookie), snapshot with { Medals = cards })
                );
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                logger.LogDebug(
                    "Medal monitoring account read failed: {ErrorType}",
                    error.GetType().Name
                );
            }
        }
        return result;
    }

    public async Task ExecuteAsync(LiveMedalMonitorTarget target, CancellationToken token)
    {
        await using var scope = scopes.CreateAsyncScope();
        var cookies = scope.ServiceProvider.GetRequiredService<CookieStrFactory<BiliCookie>>();
        if (target.Index >= cookies.Count)
            return;
        var cookie = cookies.GetCookie(target.Index);
        if (
            cookie.UserId != target.UserId.ToString()
            || CredentialVersion(cookie) != target.CredentialVersion
        )
            return;
        await scope
            .ServiceProvider.GetRequiredService<ICookieTaskGuard>()
            .EnsureValidAsync(cookie.UserId, cookie.ToString(), token);
        await scope
            .ServiceProvider.GetRequiredService<ILiveDomainService>()
            .RunFansMedalActionForAnchorAsync(
                cookie,
                target.AnchorId,
                target.RoomId,
                target.Action,
                token
            );
    }
}

public sealed class LiveMedalMonitorCycle(
    ILiveMedalMonitorSource source,
    ITaskRecordWriter records,
    ITaskFailureBatchMonitor batches,
    TimeProvider clock,
    ILogger<LiveMedalMonitorCycle> logger,
    Ray.BiliBiliTool.DomainService.LiveFansMedalExecutionGate? executionGate = null
) : IAsyncDisposable
{
    private readonly Ray.BiliBiliTool.DomainService.LiveFansMedalExecutionGate _executionGate =
        executionGate ?? new(clock);
    private readonly ConcurrentDictionary<LiveMedalMonitorTarget, Work> _active = new();
    private readonly ConcurrentDictionary<Guid, Task> _waves = new();
    private readonly ConcurrentDictionary<
        (long User, long Anchor, string Action),
        DateTimeOffset
    > _retryAfter = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _watchSlots = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _interactionSlots = new();
    private string? _executionSettings;
    private int _disposed;
    public int ActiveCount => _active.Count;

    public async Task TickAsync(LiveFansMedalTaskOptions options, CancellationToken token)
    {
        var signature = JsonSerializer.Serialize(
            options
                .ToConfigDictionary()
                .Where(pair =>
                    pair.Key
                        is not "LiveFansMedalTaskConfig:Cron"
                            and not "LiveFansMedalTaskConfig:PinnedAnchorIds"
                            and not "LiveFansMedalTaskConfig:MonitorIntervalMinutes"
                )
                .OrderBy(pair => pair.Key)
        );
        var now = clock.GetUtcNow();
        var today = now.ToOffset(TimeSpan.FromHours(8)).Date;
        if (signature != _executionSettings)
        {
            foreach (var work in _active.Values)
                work.Cancel();
            _executionSettings = signature;
            _retryAfter.Clear();
        }
        foreach (var work in _active.Values)
            if (work.Date != today)
                work.Cancel();
        foreach (var retry in _retryAfter.Where(pair => pair.Value <= now).ToArray())
            _retryAfter.TryRemove(retry.Key, out _);
        if (
            !options.IsEnable
            || !options.UseLiveStateMonitoring
            || (!options.EnableLike && !options.EnableDanmaku && !options.EnableWatch)
            || (options.OnlySelectedAnchors && options.GetIncludedAnchorIds().Count == 0)
        )
            return;
        var accounts = await source.ReadAsync(options, token);
        now = clock.GetUtcNow();
        today = now.ToOffset(TimeSpan.FromHours(8)).Date;
        foreach (var (target, work) in _active)
            if (
                work.Date != today
                || !accounts.Any(account =>
                    account.UserId == target.UserId
                    && account.CredentialVersion == target.CredentialVersion
                )
            )
                work.Cancel();
        var started = new List<Task>();
        IDisposable? batch = null;
        try
        {
            var excluded = options.GetExcludedAnchorIds();
            var selected = options.GetIncludedAnchorIds();
            foreach (var account in accounts)
            {
                var snapshot = account.Snapshot;
                if (
                    snapshot.Error is not null
                    || snapshot.UpdatedAt.ToOffset(TimeSpan.FromHours(8)).Date != today
                    || now - snapshot.UpdatedAt > TimeSpan.FromMinutes(5)
                    || snapshot.UpdatedAt - now > TimeSpan.FromMinutes(1)
                )
                    continue;
                foreach (var medal in snapshot.Medals)
                {
                    if (
                        !medal.CanInteract
                        || medal.RoomId <= 0
                        || medal.Error is not null
                        || medal.Lighted is null
                        || excluded.Contains(medal.AnchorId)
                        || (options.OnlySelectedAnchors && !selected.Contains(medal.AnchorId))
                        || (medal.Lighted == true && medal.SavingsFull)
                    )
                        continue;
                    foreach (
                        var action in medal
                            .Tasks.Where(task => !task.Done)
                            .Select(task => task.Action)
                            .Distinct()
                    )
                    {
                        if (
                            !LiveMedalCompletionEvaluator.IsActionEnabled(action, options)
                            || !LiveMedalCompletionEvaluator.CanRun(action, medal, options)
                        )
                            continue;
                        var plan = medal
                            .Tasks.First(task => task.Action == action)
                            .GetPlan(medal.Lighted, medal.SavingsFull);
                        if (
                            plan is null
                            || plan.Remaining == 0
                            || _executionGate.Remaining(
                                account.UserId.ToString(),
                                medal.AnchorId,
                                action,
                                options.GetInteractionLimit(action),
                                plan.Completed
                            ) == 0
                        )
                            continue;
                        var target = new LiveMedalMonitorTarget(
                            account.Index,
                            account.UserId,
                            account.CredentialVersion,
                            medal.AnchorId,
                            medal.RoomId,
                            action
                        );
                        if (
                            _retryAfter.TryGetValue(
                                (target.UserId, target.AnchorId, action),
                                out var retry
                            )
                            && retry > now
                        )
                            continue;
                        var work = new Work(
                            today,
                            CancellationTokenSource.CreateLinkedTokenSource(token)
                        );
                        if (!_active.TryAdd(target, work))
                        {
                            work.Dispose();
                            continue;
                        }
                        if (batch is null)
                        {
                            batch = batches.BeginBatch();
                            try
                            {
                                await batches.RecordActivityAsync(token);
                            }
                            catch (Exception)
                            {
                                logger.LogWarning("每日任务汇总活动记录暂时无法保存");
                            }
                        }
                        work.Task = RunAsync(target, work);
                        started.Add(work.Task);
                    }
                }
            }
        }
        finally
        {
            if (started.Count == 0)
                batch?.Dispose();
            else
            {
                var id = Guid.NewGuid();
                var wave = CompleteWaveAsync(started, batch!);
                _waves[id] = wave;
                _ = wave.ContinueWith(
                    _ =>
                    {
                        _waves.TryRemove(id, out var removed);
                    },
                    TaskScheduler.Default
                );
            }
        }
    }

    private static async Task CompleteWaveAsync(List<Task> work, IDisposable batch)
    {
        try
        {
            await Task.WhenAll(work);
        }
        finally
        {
            batch.Dispose();
        }
    }

    private async Task RunAsync(LiveMedalMonitorTarget target, Work work)
    {
        using var notifications = new TaskFailureNotificationScope(suppress: false);
        var slots =
            target.Action == "watchLive"
                ? _watchSlots.GetOrAdd(target.UserId, _ => new(_executionGate.WatchConcurrency))
                : _interactionSlots.GetOrAdd(target.UserId, _ => new(2));
        var acquired = false;
        try
        {
            await slots.WaitAsync(work.Token);
            acquired = true;
            await source.ExecuteAsync(target, work.Token);
            await records.WriteAsync(
                target.UserId,
                "LiveFansMedalAppService",
                null,
                TaskRecordStatus.Success,
                "本次粉丝牌互动已结束，完成情况以每日任务进度为准",
                TaskRecordTrigger.Scheduled,
                work.Token
            );
        }
        catch (OperationCanceledException) when (work.Token.IsCancellationRequested) { }
        catch (InvalidOperationException error)
            when (error.Message == "Cookie 已过期，本次活动已跳过，请在账号管理中重新登录") { }
        catch (Exception error)
        {
            logger.LogWarning(
                "Medal monitoring action failed: {Action} {ErrorType}",
                target.Action,
                error.GetType().Name
            );
            _retryAfter[(target.UserId, target.AnchorId, target.Action)] = clock
                .GetUtcNow()
                .AddMinutes(15);
            try
            {
                await records.WriteAsync(
                    target.UserId,
                    "LiveFansMedalAppService",
                    null,
                    TaskRecordStatus.Failed,
                    "粉丝牌互动执行失败，稍后自动重试",
                    TaskRecordTrigger.Scheduled,
                    CancellationToken.None
                );
            }
            catch (Exception recordError)
            {
                logger.LogWarning(
                    "Medal monitoring record failed: {ErrorType}",
                    recordError.GetType().Name
                );
            }
        }
        finally
        {
            if (acquired)
                slots.Release();
            _active.TryRemove(target, out _);
            work.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        var work = _active.Values.ToArray();
        foreach (var item in work)
            item.Cancel();
        await Task.WhenAll(work.Select(item => item.Task).Concat(_waves.Values));
        foreach (var slots in _watchSlots.Values.Concat(_interactionSlots.Values))
            slots.Dispose();
    }

    private sealed class Work(DateTime date, CancellationTokenSource cancellation) : IDisposable
    {
        public DateTime Date { get; } = date;
        public CancellationToken Token { get; } = cancellation.Token;
        public Task Task { get; set; } = Task.CompletedTask;

        public void Cancel()
        {
            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        public void Dispose() => cancellation.Dispose();
    }
}
