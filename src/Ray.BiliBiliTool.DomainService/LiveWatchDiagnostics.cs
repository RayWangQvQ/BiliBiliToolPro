using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ray.BiliBiliTool.DomainService;

public sealed class LiveWatchDiagnostics(string directory, TimeProvider? clock = null)
{
    private const long DailyByteLimit = 4 * 1024 * 1024;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _fileGate = new();
    private readonly ConcurrentDictionary<string, int> _active = new();
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private byte[]? _key;

    public Session Begin(string user, long anchor, int concurrency)
    {
        string account;
        string room;
        lock (_fileGate)
        {
            try
            {
                Directory.CreateDirectory(directory);
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(
                        directory,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                    );
                var keyPath = Path.Combine(directory, "identity.key");
                if (_key is null)
                {
                    if (!File.Exists(keyPath))
                    {
                        using var stream = new FileStream(
                            keyPath,
                            FileMode.CreateNew,
                            FileAccess.Write
                        );
                        if (!OperatingSystem.IsWindows())
                            File.SetUnixFileMode(
                                keyPath,
                                UnixFileMode.UserRead | UnixFileMode.UserWrite
                            );
                        stream.Write(RandomNumberGenerator.GetBytes(32));
                    }
                    var candidate = File.ReadAllBytes(keyPath);
                    if (candidate.Length != 32)
                        throw new InvalidDataException("Invalid diagnostic identity key");
                    _key = candidate;
                }
                account = Tag("account/" + user);
                room = Tag("room/" + user + "/" + anchor);
            }
            catch (Exception error)
                when (error is IOException or UnauthorizedAccessException or CryptographicException)
            {
                // Diagnostic storage must never interrupt an activity.
                return new(this, "", "", concurrency, enabled: false);
            }
        }
        _active.AddOrUpdate(account, 1, (_, count) => count + 1);
        var session = new Session(this, account, room, concurrency, enabled: true);
        _sessions[session.Id] = session;
        return session;
    }

    public void ObserveLiveState(
        string user,
        long anchor,
        bool live,
        DateTimeOffset? observedAt = null
    )
    {
        var observed = observedAt ?? _clock.GetUtcNow();
        if (
            _clock.GetUtcNow() - observed > TimeSpan.FromMinutes(5)
            || observed - _clock.GetUtcNow() > TimeSpan.FromMinutes(1)
        )
            return;
        string account;
        string room;
        lock (_fileGate)
        {
            if (_key is null)
                return;
            account = Tag("account/" + user);
            room = Tag("room/" + user + "/" + anchor);
        }
        foreach (var session in _sessions.Values)
            if (session.Account == account && session.Room == room)
                session.ObserveLiveState(live, observed);
    }

    private string Tag(string value) =>
        Convert.ToHexString(HMACSHA256.HashData(_key!, Encoding.UTF8.GetBytes(value)))[..16];

    private void Write(
        Session session,
        string kind,
        int? sequence = null,
        int? interval = null,
        long? driftMs = null,
        long? requestMs = null,
        int? code = null
    )
    {
        if (!session.Enabled)
            return;
        lock (_fileGate)
        {
            try
            {
                var now = _clock.GetUtcNow();
                var today = now.ToOffset(TimeSpan.FromHours(8)).Date;
                var path = Path.Combine(directory, $"watch-{today:yyyyMMdd}.jsonl");
                foreach (var old in Directory.EnumerateFiles(directory, "watch-????????.jsonl"))
                {
                    var name = Path.GetFileNameWithoutExtension(old);
                    if (
                        DateTime.TryParseExact(
                            name[6..],
                            "yyyyMMdd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var day
                        )
                        && day < today.AddDays(-6)
                    )
                        File.Delete(old);
                }
                var sample = new Sample(
                    now,
                    kind,
                    session.Id,
                    session.Account,
                    session.Room,
                    session.Concurrency,
                    _active.GetValueOrDefault(session.Account),
                    session.Live,
                    session.SentSeconds,
                    session.InitialConfirmedSeconds,
                    session.ConfirmedSeconds,
                    session.Done,
                    session.SavingsFull,
                    sequence,
                    interval,
                    driftMs,
                    requestMs,
                    code,
                    session.Outcome
                );
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sample) + "\n");
                using var stream = new FileStream(
                    path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read
                );
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                if (stream.Length + bytes.Length <= DailyByteLimit)
                    stream.Write(bytes);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Keep these records outside the notification logging pipeline.
            }
        }
    }

    public sealed record Sample(
        DateTimeOffset ObservedAt,
        string Event,
        string SessionId,
        string AccountTag,
        string RoomTag,
        int ConcurrencyLimit,
        int ActiveRooms,
        bool? Live,
        int SentSeconds,
        int InitialConfirmedSeconds,
        int ConfirmedSeconds,
        bool TaskDone,
        bool SavingsFull,
        int? Sequence,
        int? IntervalSeconds,
        long? RequestStartDriftMs,
        long? RequestDurationMs,
        int? ErrorCode,
        string Outcome
    );

    public sealed class Session : IDisposable
    {
        private readonly LiveWatchDiagnostics _owner;
        private int _disposed;
        private DateTimeOffset _liveObservedAt;

        internal Session(
            LiveWatchDiagnostics owner,
            string account,
            string room,
            int concurrency,
            bool enabled
        )
        {
            _owner = owner;
            Account = account;
            Room = room;
            Concurrency = concurrency;
            Enabled = enabled;
        }

        internal bool Enabled { get; }
        public string Id { get; } = Guid.NewGuid().ToString("N");
        internal string Account { get; }
        internal string Room { get; }
        internal int Concurrency { get; }
        internal bool? Live { get; private set; }
        internal int SentSeconds { get; private set; }
        internal int InitialConfirmedSeconds { get; private set; }
        internal int ConfirmedSeconds { get; private set; }
        internal bool Done { get; private set; }
        internal bool SavingsFull { get; private set; }
        internal string Outcome { get; private set; } = "pending";

        public void Start(bool live, int confirmed, bool done, bool savingsFull)
        {
            Live = live;
            _liveObservedAt = _owner._clock.GetUtcNow();
            InitialConfirmedSeconds = ConfirmedSeconds = confirmed;
            Done = done;
            SavingsFull = savingsFull;
            _owner.Write(this, "start");
        }

        public void Entry(int code, long requestMs) =>
            _owner.Write(this, "entry", sequence: 0, requestMs: requestMs, code: code);

        public void Heartbeat(
            int sequence,
            int interval,
            long driftMs,
            long requestMs,
            int code,
            bool accepted
        )
        {
            if (accepted)
                SentSeconds += interval;
            _owner.Write(
                this,
                accepted ? "heartbeat" : "heartbeat_rejected",
                sequence,
                interval,
                driftMs,
                requestMs,
                code
            );
        }

        public void Progress(int confirmed, bool done, bool savingsFull)
        {
            var changed =
                confirmed != ConfirmedSeconds || done != Done || savingsFull != SavingsFull;
            ConfirmedSeconds = confirmed;
            Done = done;
            SavingsFull = savingsFull;
            if (changed)
                _owner.Write(this, "progress");
        }

        public void Finish(string outcome) => Outcome = outcome;

        internal void ObserveLiveState(bool live, DateTimeOffset observedAt)
        {
            if (Volatile.Read(ref _disposed) != 0 || observedAt < _liveObservedAt)
                return;
            _liveObservedAt = observedAt;
            if (Live == live)
                return;
            Live = live;
            _owner.Write(this, "live_state");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            _owner.Write(this, "end");
            if (Enabled)
            {
                _owner._sessions.TryRemove(Id, out _);
                _owner._active.AddOrUpdate(Account, 0, (_, count) => Math.Max(0, count - 1));
            }
        }
    }
}
