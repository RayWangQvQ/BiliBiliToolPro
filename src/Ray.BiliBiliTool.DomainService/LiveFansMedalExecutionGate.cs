using System.Collections.Concurrent;
using System.Text.Json;
using Ray.BiliBiliTool.Agent;

namespace Ray.BiliBiliTool.DomainService;

public sealed class LiveFansMedalExecutionGate(
    TimeProvider? clock = null,
    string? budgetPath = null
)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<(string User, long Anchor, string Action), byte> _active =
        new();
    private readonly object _budgetGate = new();
    private readonly Dictionary<string, (DateTime Date, int Sent)> _dailyLikes = new();
    private readonly Dictionary<(string User, long Anchor, string Action), DailyUsage> _usage =
        new();
    private bool _loaded;

    // Watch one room at a time per account, shared by monitoring and recovery.
    public int WatchConcurrency => 1;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _watchSlots = new();

    public async Task<IDisposable> AcquireWatchSlotAsync(string user, CancellationToken token)
    {
        var slots = _watchSlots.GetOrAdd(user, _ => new(WatchConcurrency));
        await slots.WaitAsync(token);
        return new WatchLease(slots);
    }

    private sealed class WatchLease(SemaphoreSlim slots) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                slots.Release();
        }
    }

    public int Remaining(string user, long anchor, string action, int limit, int completed = 0)
    {
        lock (_budgetGate)
        {
            Load();
            var key = (user, anchor, action);
            var today = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
            var usage = _usage.GetValueOrDefault(key);
            var previous = usage;
            if (usage?.Date != today)
                usage = new(user, anchor, action, today, 0, 0, 0);
            if (
                !_active.ContainsKey(key)
                && usage.PendingSince is { } since
                && _clock.GetUtcNow() - since >= TimeSpan.FromMinutes(5)
            )
                usage = usage with { Count = usage.Confirmed ?? 0, PendingSince = null };
            var confirmed = Math.Max(Math.Max(0, completed), usage.Confirmed ?? 0);
            var used = Math.Max(confirmed, usage.Count);
            _usage[key] = usage with
            {
                Count = used,
                Confirmed = confirmed,
                PendingSince = used > confirmed ? usage.PendingSince : null,
            };
            if (_usage[key] != previous)
                Save();
            return Math.Max(0, limit - used);
        }
    }

    public int Reserve(
        string user,
        long anchor,
        string action,
        int limit,
        int requested,
        bool whole = false
    )
    {
        lock (_budgetGate)
        {
            var remaining = Remaining(user, anchor, action, limit);
            if (action is "sendDanmu" or "watchLive")
                remaining = Math.Min(remaining, RemainingProtection(user, anchor, action));
            var amount = Math.Min(Math.Max(0, requested), remaining);
            if (whole && amount < requested)
                return 0;
            if (amount == 0)
                return 0;
            var key = (user, anchor, action);
            var usage = _usage[key];
            _usage[key] = usage with
            {
                Count = usage.Count + amount,
                Sent = (usage.Sent ?? 0) + amount,
                PendingSince = usage.PendingSince ?? _clock.GetUtcNow(),
            };
            Save();
            return amount;
        }
    }

    internal void ReleaseWatchReservation(string user, long anchor, int seconds, DateTime date) =>
        ReleaseReservation(user, anchor, "watchLive", seconds, date);

    private void ReleaseReservation(
        string user,
        long anchor,
        string action,
        int amount,
        DateTime date
    )
    {
        lock (_budgetGate)
        {
            Load();
            var key = (user, anchor, action);
            if (_usage.TryGetValue(key, out var usage) && usage.Date == date)
            {
                var count = Math.Max(usage.Confirmed ?? 0, usage.Count - Math.Max(0, amount));
                _usage[key] = usage with
                {
                    Count = count,
                    Sent = Math.Max(0, (usage.Sent ?? 0) - Math.Max(0, amount)),
                    PendingSince = count > (usage.Confirmed ?? 0) ? usage.PendingSince : null,
                };
                Save();
            }
        }
    }

    public int ReserveInteraction(
        BiliCookie cookie,
        long anchor,
        string action,
        int limit,
        int requested,
        out DateTime date
    )
    {
        lock (_budgetGate)
        {
            date = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
            var amount = Math.Min(
                Math.Max(0, requested),
                Remaining(cookie.UserId, anchor, action, limit)
            );
            if (action == "like")
            {
                var budget = _dailyLikes.GetValueOrDefault(cookie.UserId);
                if (budget.Date != date)
                    budget = (date, 0);
                amount = Math.Min(amount, Math.Max(0, 5000 - budget.Sent));
                _dailyLikes[cookie.UserId] = (date, budget.Sent + amount);
            }
            return Reserve(cookie.UserId, anchor, action, limit, amount);
        }
    }

    public void ReleaseRejectedInteraction(
        BiliCookie cookie,
        long anchor,
        string action,
        int amount,
        DateTime date
    )
    {
        lock (_budgetGate)
        {
            ReleaseReservation(cookie.UserId, anchor, action, amount, date);
            if (
                action == "like"
                && _dailyLikes.TryGetValue(cookie.UserId, out var budget)
                && budget.Date == date
            )
                _dailyLikes[cookie.UserId] = (date, Math.Max(0, budget.Sent - Math.Max(0, amount)));
        }
    }

    private void Load()
    {
        if (_loaded)
            return;
        var migrated = false;
        if (budgetPath is not null && File.Exists(budgetPath))
            foreach (
                var item in JsonSerializer.Deserialize<DailyUsage[]>(File.ReadAllText(budgetPath))
                    ?? throw new InvalidOperationException("粉丝牌执行额度记录暂不可用")
            )
            {
                if (
                    item.Count < 0
                    || item.Confirmed < 0
                    || item.Sent < 0
                    || item.Confirmed > item.Count
                )
                    throw new InvalidOperationException("粉丝牌执行额度记录暂不可用");
                // Legacy counts represent requests, not confirmed platform progress.
                migrated |= item.Confirmed is null || item.Sent is null;
                _usage[(item.User, item.Anchor, item.Action)] = item with
                {
                    Confirmed = item.Confirmed ?? 0,
                    Sent = item.Sent ?? item.Count,
                    PendingSince =
                        item.Count > (item.Confirmed ?? 0)
                            ? item.PendingSince ?? _clock.GetUtcNow()
                            : null,
                };
            }
        var today = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
        foreach (
            var group in _usage
                .Values.Where(item => item.Date == today && item.Action == "like")
                .GroupBy(item => item.User)
        )
            _dailyLikes[group.Key] = (
                today,
                (int)Math.Min(5000L, group.Sum(item => (long)(item.Sent ?? item.Count)))
            );
        _loaded = true;
        if (migrated)
            Save();
    }

    private void Save()
    {
        if (budgetPath is null)
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(budgetPath)!);
        var temporary = budgetPath + ".tmp";
        var today = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
        File.WriteAllText(
            temporary,
            JsonSerializer.Serialize(_usage.Values.Where(item => item.Date == today))
        );
        File.Move(temporary, budgetPath, overwrite: true);
    }

    public sealed record DailyUsage(
        string User,
        long Anchor,
        string Action,
        DateTime Date,
        int Count,
        int? Confirmed = null,
        int? Sent = null,
        DateTimeOffset? PendingSince = null
    );

    public (int Confirmed, int Pending) Progress(string user, long anchor, string action)
    {
        lock (_budgetGate)
        {
            Remaining(user, anchor, action, int.MaxValue);
            var usage = _usage[(user, anchor, action)];
            return (usage.Confirmed ?? 0, Math.Max(0, usage.Count - (usage.Confirmed ?? 0)));
        }
    }

    public int RemainingProtection(string user, long anchor, string action)
    {
        lock (_budgetGate)
        {
            Remaining(user, anchor, action, int.MaxValue);
            return Math.Max(
                0,
                (action == "sendDanmu" ? 100 : 86400) - (_usage[(user, anchor, action)].Sent ?? 0)
            );
        }
    }

    public void Confirm(string user, long anchor, string action, int completed, bool done = false)
    {
        lock (_budgetGate)
        {
            Remaining(user, anchor, action, int.MaxValue, completed);
            var key = (user, anchor, action);
            if (done && _usage[key].Count > (_usage[key].Confirmed ?? 0))
            {
                _usage[key] = _usage[key] with
                {
                    Count = _usage[key].Confirmed ?? 0,
                    PendingSince = null,
                };
                Save();
            }
        }
    }

    public int ReserveMonitoredLikes(BiliCookie cookie, int requested)
    {
        lock (_budgetGate)
        {
            Load();
            var today = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
            var budget = _dailyLikes.GetValueOrDefault(cookie.UserId);
            if (budget.Date != today)
                budget = (today, 0);
            var amount = Math.Min(Math.Max(0, requested), 5000 - budget.Sent);
            _dailyLikes[cookie.UserId] = (today, budget.Sent + amount);
            return amount;
        }
    }

    public int RemainingMonitoredLikes(BiliCookie cookie)
    {
        lock (_budgetGate)
        {
            Load();
            var today = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
            var budget = _dailyLikes.GetValueOrDefault(cookie.UserId);
            return budget.Date == today ? Math.Max(0, 5000 - budget.Sent) : 5000;
        }
    }

    public IDisposable? TryAcquire(BiliCookie cookie, long anchor, string action)
    {
        var key = (cookie.UserId, anchor, action);
        if (!_active.TryAdd(key, 0))
            return null;
        try
        {
            lock (_budgetGate)
            {
                Remaining(cookie.UserId, anchor, action, int.MaxValue);
                var usage = _usage[key];
                // Reconcile fresh platform progress before retrying aged, unconfirmed requests.
                // Retain the raw account protection counter across retries and restarts.
                if (
                    usage.PendingSince is { } since
                    && _clock.GetUtcNow() - since >= TimeSpan.FromMinutes(5)
                )
                {
                    _usage[key] = usage with { Count = usage.Confirmed ?? 0, PendingSince = null };
                    Save();
                }
            }
            return new Lease(_active, key);
        }
        catch
        {
            _active.TryRemove(key, out _);
            throw;
        }
    }

    private sealed class Lease(
        ConcurrentDictionary<(string User, long Anchor, string Action), byte> active,
        (string User, long Anchor, string Action) key
    ) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                active.TryRemove(key, out _);
        }
    }
}
