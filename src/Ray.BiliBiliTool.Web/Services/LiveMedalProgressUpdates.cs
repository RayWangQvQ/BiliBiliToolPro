using System.Security.Cryptography;
using System.Text;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.Web.Services;

public sealed class LiveMedalProgressUpdates(
    ILogger<LiveMedalProgressUpdates> logger,
    LiveWatchDiagnostics? watchDiagnostics = null
) : ILiveFansMedalProgressObserver
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LiveMedalSnapshot> _snapshots = [];
    private readonly Dictionary<string, Dictionary<Guid, Action<LiveMedalSnapshot>>> _listeners =
    [];
    private long _revision;

    // Device-cookie enrichment must not change the account's progress subscription.
    public static string AccountKey(BiliCookie cookie) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes($"{cookie.UserId}\n{cookie.SessData}\n{cookie.BiliJct}")
            )
        );

    public LiveMedalSnapshot? Get(BiliCookie cookie)
    {
        lock (_gate)
        {
            var snapshot = _snapshots.GetValueOrDefault(AccountKey(cookie));
            return snapshot?.UpdatedAt > DateTimeOffset.UtcNow.AddDays(-7) ? snapshot : null;
        }
    }

    public IDisposable Subscribe(string accountKey, Action<LiveMedalSnapshot> listener)
    {
        var id = Guid.NewGuid();
        lock (_gate)
        {
            if (!_listeners.TryGetValue(accountKey, out var entries))
                _listeners[accountKey] = entries = [];
            entries.Add(id, listener);
        }
        return new Subscription(this, accountKey, id);
    }

    public LiveMedalSnapshot Publish(BiliCookie cookie, LiveMedalSnapshot snapshot)
    {
        var key = AccountKey(cookie);
        Action<LiveMedalSnapshot>[] listeners;
        lock (_gate)
        {
            foreach (
                var old in _snapshots
                    .Where(pair => pair.Value.UpdatedAt < DateTimeOffset.UtcNow.AddDays(-7))
                    .Select(pair => pair.Key)
                    .ToArray()
            )
                _snapshots.Remove(old);
            var previous = _snapshots.GetValueOrDefault(key);
            var cards = snapshot
                .Medals.Select(card =>
                {
                    var existing = previous?.Medals.FirstOrDefault(item =>
                        item.AnchorId == card.AnchorId
                    );
                    return
                        existing?.ProgressUpdatedAt > (card.ProgressUpdatedAt ?? snapshot.UpdatedAt)
                        ? existing with
                        {
                            Live = card.Live,
                            CanInteract = card.CanInteract,
                            RoomId = card.RoomId,
                        }
                        : card;
                })
                .ToArray();
            snapshot = snapshot with { Medals = cards, Revision = ++_revision };
            _snapshots[key] = snapshot;
            listeners = Listeners(key);
        }
        foreach (var card in snapshot.Medals)
            watchDiagnostics?.ObserveLiveState(
                cookie.UserId,
                card.AnchorId,
                card.Live,
                snapshot.UpdatedAt
            );
        Notify(listeners, snapshot);
        return snapshot;
    }

    public void Report(
        BiliCookie cookie,
        long anchorId,
        ActivatedMedalResponse progress,
        DateTimeOffset observedAt
    )
    {
        var now = DateTimeOffset.UtcNow;
        if (
            observedAt.ToOffset(TimeSpan.FromHours(8)).Date
                != now.ToOffset(TimeSpan.FromHours(8)).Date
            || observedAt - now > TimeSpan.FromMinutes(1)
        )
            return;
        var key = AccountKey(cookie);
        LiveMedalSnapshot snapshot;
        Action<LiveMedalSnapshot>[] listeners;
        lock (_gate)
        {
            if (!_snapshots.TryGetValue(key, out snapshot!))
                return;
            var card = snapshot.Medals.FirstOrDefault(item => item.AnchorId == anchorId);
            if (card is null || observedAt < (card.ProgressUpdatedAt ?? snapshot.UpdatedAt))
                return;
            var updated = card with
            {
                Level = progress.Level > 0 ? progress.Level : card.Level,
                Lighted = progress.Is_lighted,
                SavingsFull = progress.Reach_free_intimacy_limit,
                Tasks = progress.Task_info.Select(LiveMedalTaskProgress.From).ToArray(),
                Error = null,
                ProgressUpdatedAt = observedAt,
            };
            snapshot = snapshot with
            {
                Medals = snapshot
                    .Medals.Select(item => item.AnchorId == anchorId ? updated : item)
                    .ToArray(),
                Revision = ++_revision,
            };
            _snapshots[key] = snapshot;
            listeners = Listeners(key);
        }
        Notify(listeners, snapshot);
    }

    private Action<LiveMedalSnapshot>[] Listeners(string key) =>
        _listeners.TryGetValue(key, out var entries) ? entries.Values.ToArray() : [];

    private void Notify(Action<LiveMedalSnapshot>[] listeners, LiveMedalSnapshot snapshot)
    {
        foreach (var listener in listeners)
            try
            {
                listener(snapshot);
            }
            catch (Exception error)
            {
                logger.LogDebug(
                    "Medal progress subscriber failed: {ErrorType}",
                    error.GetType().Name
                );
            }
    }

    private sealed class Subscription(LiveMedalProgressUpdates owner, string key, Guid id)
        : IDisposable
    {
        public void Dispose()
        {
            lock (owner._gate)
                if (owner._listeners.TryGetValue(key, out var entries))
                {
                    entries.Remove(id);
                    if (entries.Count == 0)
                        owner._listeners.Remove(key);
                }
        }
    }
}
