using System.Text.Json;
using Ray.BiliBiliTool.DomainService;
using Xunit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public sealed class LiveWatchDiagnosticsTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly LiveFansMedalTaskTests.BudgetClock _clock = new()
    {
        Now = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.Zero),
    };

    private LiveWatchDiagnostics.Sample[] Samples() =>
        Directory
            .GetFiles(_path, "watch-*.jsonl")
            .SelectMany(File.ReadLines)
            .Select(line => JsonSerializer.Deserialize<LiveWatchDiagnostics.Sample>(line)!)
            .ToArray();

    [Fact]
    public void AcceptedHeartbeatsDoNotConfirmPlatformProgress()
    {
        var diagnostics = new LiveWatchDiagnostics(_path, _clock);
        using (var session = diagnostics.Begin("private-user-id", 123456789012, 8))
        {
            session.Start(true, 1800, false, false);
            session.Heartbeat(1, 60, 4, 500, 0, accepted: true);
            session.Heartbeat(2, 60, 7, 400, 1012002, accepted: false);
            session.Progress(1800, false, false);
        }
        var end = Samples().Last();
        Assert.Equal(60, end.SentSeconds);
        Assert.Equal(1800, end.ConfirmedSeconds);
        Assert.False(end.TaskDone);
        Assert.Equal("pending", end.Outcome);
        Assert.Equal(
            1012002,
            Samples().Single(sample => sample.Event == "heartbeat_rejected").ErrorCode
        );
        var text = string.Join(
            '\n',
            Directory.GetFiles(_path, "watch-*.jsonl").SelectMany(File.ReadLines)
        );
        Assert.DoesNotContain("private-user-id", text);
        Assert.DoesNotContain("123456789012", text);
    }

    [Fact]
    public void AnonymousTagsSurviveRestartsAndDistinguishAccountsAndRooms()
    {
        using (var first = new LiveWatchDiagnostics(_path, _clock).Begin("first", 1, 8))
            first.Start(false, 0, false, false);
        using (var second = new LiveWatchDiagnostics(_path, _clock).Begin("first", 1, 8))
            second.Start(false, 0, false, false);
        using (var third = new LiveWatchDiagnostics(_path, _clock).Begin("second", 1, 8))
            third.Start(false, 0, false, false);
        var starts = Samples().Where(sample => sample.Event == "start").ToArray();
        Assert.Equal(starts[0].AccountTag, starts[1].AccountTag);
        Assert.Equal(starts[0].RoomTag, starts[1].RoomTag);
        Assert.NotEqual(starts[0].SessionId, starts[1].SessionId);
        Assert.NotEqual(starts[0].AccountTag, starts[2].AccountTag);
        Assert.NotEqual(starts[0].RoomTag, starts[2].RoomTag);
    }

    [Fact]
    public void ActiveCountsArePerAccountAndDisposalIsIdempotent()
    {
        var diagnostics = new LiveWatchDiagnostics(_path, _clock);
        var first = diagnostics.Begin("first", 1, 8);
        var second = diagnostics.Begin("first", 2, 8);
        using var other = diagnostics.Begin("second", 1, 8);
        first.Start(true, 0, false, false);
        other.Start(true, 0, false, false);
        first.Dispose();
        first.Dispose();
        second.Start(true, 0, false, false);
        second.Dispose();
        var samples = Samples();
        Assert.Equal(2, samples.First().ActiveRooms);
        Assert.Equal(1, samples.Single(sample => sample.SessionId == other.Id).ActiveRooms);
        Assert.Equal(
            1,
            samples
                .Single(sample => sample.SessionId == second.Id && sample.Event == "start")
                .ActiveRooms
        );
        Assert.Single(
            samples.Where(sample => sample.SessionId == first.Id && sample.Event == "end")
        );
    }

    [Fact]
    public void RecordsRotateAtLocalMidnightAndExpireAfterSevenDays()
    {
        var diagnostics = new LiveWatchDiagnostics(_path, _clock);
        using (var first = diagnostics.Begin("first", 1, 8))
            first.Start(true, 0, false, false);
        File.WriteAllText(Path.Combine(_path, "watch-20260928.jsonl"), "old");
        File.WriteAllText(Path.Combine(_path, "watch-20260930.jsonl"), "keep");
        _clock.Now = _clock.Now.AddHours(2);
        using (var second = diagnostics.Begin("first", 1, 8))
            second.Start(true, 0, false, false);
        Assert.False(File.Exists(Path.Combine(_path, "watch-20260928.jsonl")));
        Assert.False(File.Exists(Path.Combine(_path, "watch-20260930.jsonl")));
        Assert.True(File.Exists(Path.Combine(_path, "watch-20261006.jsonl")));
        Assert.True(File.Exists(Path.Combine(_path, "watch-20261007.jsonl")));
    }

    [Fact]
    public void DailyDiskUsageIsBounded()
    {
        var diagnostics = new LiveWatchDiagnostics(_path, _clock);
        using var session = diagnostics.Begin("first", 1, 8);
        var path = Path.Combine(_path, "watch-20261006.jsonl");
        using (var stream = File.Create(path))
            stream.SetLength(4 * 1024 * 1024);
        session.Start(true, 0, false, false);
        session.Heartbeat(1, 60, 0, 0, 0, true);
        Assert.Equal(4 * 1024 * 1024, new FileInfo(path).Length);
    }

    [Fact]
    public void ExistingRoomSnapshotsRecordLiveTransitionsWithoutDuplicates()
    {
        var diagnostics = new LiveWatchDiagnostics(_path, _clock);
        var session = diagnostics.Begin("first", 1, 8);
        session.Start(true, 0, false, false);
        diagnostics.ObserveLiveState("different", 1, false);
        diagnostics.ObserveLiveState("first", 1, false, _clock.Now.AddMinutes(-10));
        Assert.DoesNotContain(Samples(), sample => sample.Event == "live_state");
        diagnostics.ObserveLiveState("first", 1, false);
        diagnostics.ObserveLiveState("first", 1, false);
        session.Dispose();
        diagnostics.ObserveLiveState("first", 1, true);
        var change = Samples().Single(sample => sample.Event == "live_state");
        Assert.False(change.Live);
        Assert.Equal(session.Id, change.SessionId);
        Assert.False(Samples().Last().Live);
    }

    [Fact]
    public void UnwritableStorageDoesNotInterruptActivities()
    {
        File.WriteAllText(_path, "not a directory");
        using var session = new LiveWatchDiagnostics(_path, _clock).Begin("first", 1, 8);
        session.Start(true, 0, false, false);
        session.Heartbeat(1, 60, 0, 0, 0, true);
        session.Finish("completed");
        Assert.Equal("not a directory", File.ReadAllText(_path));
    }

    [Fact]
    public async Task SingleRoomModeQueuesSameAccountButKeepsAccountsIndependent()
    {
        var gate = new LiveFansMedalExecutionGate(watchConcurrency: 1);
        var first = await gate.AcquireWatchSlotAsync("first", CancellationToken.None);
        var waiting = gate.AcquireWatchSlotAsync("first", CancellationToken.None);
        Assert.False(waiting.IsCompleted);
        using var other = await gate.AcquireWatchSlotAsync("second", CancellationToken.None);
        first.Dispose();
        first.Dispose();
        using var next = await waiting.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CanceledQueueDoesNotConsumeOrLeakWatchSlots()
    {
        var gate = new LiveFansMedalExecutionGate(watchConcurrency: 1);
        var first = await gate.AcquireWatchSlotAsync("first", CancellationToken.None);
        using var cancel = new CancellationTokenSource();
        var waiting = gate.AcquireWatchSlotAsync("first", cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        first.Dispose();
        using var next = await gate.AcquireWatchSlotAsync("first", CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        if (Directory.Exists(_path))
            Directory.Delete(_path, recursive: true);
        else if (File.Exists(_path))
            File.Delete(_path);
    }
}
