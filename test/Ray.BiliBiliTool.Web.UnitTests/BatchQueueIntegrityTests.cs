using System.Collections.Concurrent;
using System.Reflection;
using Ray.Serilog.Sinks.Batched;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace Ray.BiliBiliTool.Web.UnitTests;

[Collection("Notification configuration diagnostics")]
public class BatchQueueIntegrityTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(2, 5)]
    [InlineData(3, 100)]
    public async Task FlushAll_DeliversEveryQueuedEventExactlyOnce(int limit, int count)
    {
        using var sink = new RecordingSink(limit);
        Seed(sink, "group", count);
        await sink.FlushAllAsync("title");
        Assert.Equal(Enumerable.Range(0, count), sink.Received.Select(item => item.Id));
        Assert.All(sink.Batches, batch => Assert.InRange(batch, 1, limit));
        Assert.All(sink.Received, item => Assert.Equal("title", item.Title));
        await sink.FlushAllAsync();
        Assert.Equal(count, sink.Received.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoggerShutdown_DrainsEveryGroupBeforeReturning(bool asynchronous)
    {
        var sink = new RecordingSink(2);
        Seed(sink, "first", 5);
        Seed(sink, "second", 7);
        var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        if (asynchronous)
            await logger.DisposeAsync();
        else
            logger.Dispose();
        Assert.Equal(12, sink.Received.Count);
        Assert.Equal(5, sink.Received.Count(item => item.Group == "first"));
        Assert.Equal(7, sink.Received.Count(item => item.Group == "second"));
        Assert.True(sink.IsDisposed);
    }

    [Fact]
    public async Task ManagerFlush_DrainsOnlyRequestedGroupThenPreservesOtherGroups()
    {
        using var sink = new RecordingSink(2);
        Seed(sink, "requested", 5);
        Seed(sink, "other", 3);
        await BatchSinkManager.FlushAsync("requested", "selected-title");
        Assert.Equal(5, sink.Received.Count);
        Assert.All(sink.Received, item => Assert.Equal("requested", item.Group));
        await sink.FlushAllAsync("remaining-title");
        Assert.Equal(8, sink.Received.Count);
    }

    [Fact]
    public async Task ExplicitGroupFlush_DrainsAllBatchesAndCanBeRepeated()
    {
        using var sink = new RecordingSink(2);
        Seed(sink, "group", 5);
        await sink.FlushAsync("group");
        Assert.Equal(Enumerable.Range(0, 5), sink.Received.Select(item => item.Id));
        await sink.FlushAsync("group");
        Assert.Equal(5, sink.Received.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidBatchLimit_IsRejectedBeforeRegistration(int limit)
    {
        var before = BatchSinkManager.RegisteredSinkCount;
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecordingSink(limit));
        Assert.Equal(before, BatchSinkManager.RegisteredSinkCount);
    }

    [Fact]
    public async Task ConcurrentEmitAndFlush_PreservesEveryEventExactlyOnce()
    {
        using var sink = new RecordingSink(7);
        const int count = 2400;
        await Task.WhenAll(
            Enumerable
                .Range(0, 8)
                .Select(async group =>
                {
                    for (var index = group; index < count; index += 8)
                    {
                        var item = Event(index);
                        item.AddPropertyIfAbsent(
                            new(
                                Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey,
                                new ScalarValue($"group-{group}")
                            )
                        );
                        sink.Emit(item);
                        if (index % 13 == 0)
                            await sink.FlushAsync($"group-{group}");
                    }
                })
        );
        await sink.FlushAllAsync();
        Assert.Equal(Enumerable.Range(0, count), sink.Received.Select(item => item.Id).Order());
        Assert.All(sink.Batches, batch => Assert.InRange(batch, 1, 7));
    }

    [Fact]
    public async Task EmitDuringFlush_PreservesTheNewEventForTheNextFlush()
    {
        using var sink = new RecordingSink(2);
        Seed(sink, "group", 3);
        var inserted = false;
        sink.BeforeBatch = _ =>
        {
            if (!inserted)
            {
                inserted = true;
                var item = Event(99);
                item.AddPropertyIfAbsent(
                    new(
                        Ray.Serilog.Sinks.Batched.Constants.GroupPropertyKey,
                        new ScalarValue("group")
                    )
                );
                sink.Emit(item);
            }
            return Task.CompletedTask;
        };
        await sink.FlushAsync("group");
        Assert.Equal(new[] { 0, 1, 2 }, sink.Received.Select(item => item.Id));
        await sink.FlushAsync("group");
        Assert.Equal(new[] { 0, 1, 2, 99 }, sink.Received.Select(item => item.Id));
    }

    [Fact]
    public async Task FailedBatch_IsReportedWithoutRetryingItOrDiscardingTheUnsentTail()
    {
        using var sink = new RecordingSink(2);
        Seed(sink, "group", 5);
        sink.BeforeBatch = _ => Task.FromException(new HttpRequestException("synthetic failure"));
        await Assert.ThrowsAsync<HttpRequestException>(() => sink.FlushAsync("group"));
        Assert.Empty(sink.Received);
        sink.BeforeBatch = null;
        await sink.FlushAsync("group");
        Assert.Equal(new[] { 2, 3, 4 }, sink.Received.Select(item => item.Id));
        await sink.FlushAsync("group");
        Assert.Equal(3, sink.Received.Count);
    }

    [Fact]
    public async Task ConcurrentDisposal_WaitsForPendingSendAndDoesNotDuplicateEvents()
    {
        var before = BatchSinkManager.RegisteredSinkCount;
        var sink = new RecordingSink(2);
        Seed(sink, "group", 5);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sink.BeforeBatch = async _ =>
        {
            started.TrySetResult();
            await release.Task;
        };
        var flushing = sink.FlushAsync("group");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var disposals = Enumerable.Range(0, 4).Select(_ => Task.Run(sink.Dispose)).ToArray();
        Assert.False(Task.WhenAll(disposals).IsCompleted);
        release.SetResult();
        await Task.WhenAll(disposals.Append(flushing)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(sink.IsDisposed);
        Assert.Equal(Enumerable.Range(0, 5), sink.Received.Select(item => item.Id));
        Assert.Equal(before, BatchSinkManager.RegisteredSinkCount);
        await sink.FlushAllAsync();
        await sink.FlushAsync("group");
        sink.Emit(Event(100));
        sink.Dispose();
        Assert.Equal(5, sink.Received.Count);
    }

    [Fact]
    public async Task Dispose_WaitsForFlushWithoutPostingToCallerSynchronizationContext()
    {
        var sink = new RecordingSink(2);
        Seed(sink, "group", 5);
        sink.BeforeBatch = async _ => await Task.Delay(10);
        await Task.Run(() =>
            {
                var original = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(new RejectPostingContext());
                try
                {
                    sink.Dispose();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(original);
                }
            })
            .WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(5, sink.Received.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FormattingFilteringAndDefaultGroup_RemainCompatible(bool combined)
    {
        using var sink = new MessageSink(combined);
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        logger.Information("filtered");
        logger.Warning("经验+5 √");
        logger.Warning("second");
        await sink.FlushAsync("Unknown", "batch-title");
        Assert.Equal(combined ? 1 : 2, sink.Messages.Count);
        Assert.Contains("经验+5 ✔", string.Concat(sink.Messages.Select(item => item.Message)));
        Assert.DoesNotContain(
            "filtered",
            string.Concat(sink.Messages.Select(item => item.Message))
        );
        Assert.All(
            sink.Messages,
            item => Assert.Equal(combined ? "batch-title" : "推送", item.Title)
        );
    }

    // Populate a stable queue to reproduce a flush boundary without automatic-flush races.
    private static void Seed(RecordingSink sink, string group, int count)
    {
        var queues =
            (ConcurrentDictionary<string, ConcurrentQueue<LogEvent>>)
                typeof(BatchedSink)
                    .GetField("_groupLogEvents", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(sink)!;
        queues[group] = new ConcurrentQueue<LogEvent>(Enumerable.Range(0, count).Select(Event));
    }

    private static LogEvent Event(int id) =>
        new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse("event {Id}"),
            [new LogEventProperty("Id", new ScalarValue(id))]
        );

    private sealed class RecordingSink(int limit) : BatchedSink(batchSizeLimit: limit)
    {
        public List<(int Id, string Group, string Title)> Received { get; } = [];
        public List<int> Batches { get; } = [];
        public Func<LogEvent[], Task>? BeforeBatch { get; set; }
        protected override IPushService PushService => throw new InvalidOperationException();

        protected override async Task EmitBatchAsync(
            IEnumerable<LogEvent> events,
            string jobId,
            string title = ""
        )
        {
            var batch = events.ToArray();
            if (BeforeBatch is not null)
                await BeforeBatch(batch);
            Batches.Add(batch.Length);
            Received.AddRange(
                batch.Select(item =>
                    ((int)((ScalarValue)item.Properties["Id"]).Value!, jobId, title)
                )
            );
        }
    }

    private sealed class RejectPostingContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) =>
            throw new InvalidOperationException("Disposal captured caller context");
    }

    private sealed class MessageSink(bool combined)
        : BatchedSink(
            sendBatchesAsOneMessages: combined,
            minimumLogEventLevel: LogEventLevel.Warning
        )
    {
        public List<(string Message, string Title)> Messages { get; } = [];
        protected override IPushService PushService => throw new InvalidOperationException();

        protected override Task PushMessageAsync(string message, string title = "推送")
        {
            Messages.Add((message, title));
            return Task.CompletedTask;
        }
    }
}
