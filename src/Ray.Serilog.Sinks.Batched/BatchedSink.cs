using System.Collections.Concurrent;
using System.Text;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Ray.Serilog.Sinks.Batched;

public abstract class BatchedSink : ILogEventSink, IDisposable, IBatchSink
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<LogEvent>> _groupLogEvents =
        new();

    private readonly int _batchSizeLimit;
    private readonly LogEventLevel _minimumLogEventLevel;
    private readonly bool _sendBatchesAsOneMessages;
    private readonly ITextFormatter _formatter;
    private readonly object _syncRoot = new();
    private readonly SemaphoreSlim _flushSemaphore;
    private volatile bool _disposed;
    private bool _disposing;
    private readonly TaskCompletionSource<object?> _disposeCompletion = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    protected BatchedSink(
        bool sendBatchesAsOneMessages,
        int batchSizeLimit,
        IFormatProvider formatProvider,
        LogEventLevel minimumLogEventLevel
    )
        : this(sendBatchesAsOneMessages, batchSizeLimit, null, formatProvider, minimumLogEventLevel)
    { }

    protected BatchedSink(
        bool sendBatchesAsOneMessages = true,
        int batchSizeLimit = int.MaxValue,
        string? outputTemplate = "{Message:lj}{NewLine}{Exception}",
        IFormatProvider? formatProvider = null,
        LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSizeLimit);
        _minimumLogEventLevel = minimumLogEventLevel;
        _sendBatchesAsOneMessages = sendBatchesAsOneMessages;
        _batchSizeLimit = batchSizeLimit;

        outputTemplate = string.IsNullOrWhiteSpace(outputTemplate)
            ? Constants.DefaultOutputTemplate
            : outputTemplate;
        _formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

        _flushSemaphore = new SemaphoreSlim(1, 1);

        BatchSinkManager.RegisterSink(this);
    }

    public virtual void Emit(LogEvent logEvent)
    {
        if (_disposed)
            return;
        ArgumentNullException.ThrowIfNull(logEvent);
        try
        {
            string groupKey = "Unknown";
            bool shouldFlush;
            lock (_syncRoot)
            {
                if (_disposed || _disposing || logEvent.Level < _minimumLogEventLevel)
                    return;
                if (logEvent.Properties.TryGetValue(Constants.GroupPropertyKey, out var property))
                {
                    var value = property.ToString().Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(value))
                        groupKey = value;
                }
                var queue = _groupLogEvents.GetOrAdd(
                    groupKey,
                    _ => new ConcurrentQueue<LogEvent>()
                );
                queue.Enqueue(logEvent);
                shouldFlush = queue.Count > _batchSizeLimit;
            }
            if (shouldFlush)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await FlushAsync(groupKey).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        SelfLog.WriteLine(
                            "Automatic notification flush failed ({0}).",
                            exception.GetType().Name
                        );
                    }
                });
        }
        catch (Exception exception)
        {
            SelfLog.WriteLine("Notification enqueue failed ({0}).", exception.GetType().Name);
        }
    }

    public async Task FlushAsync(string jobId, string title = "")
    {
        if (_disposed)
            return;
        await _flushSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
                return;
            ConcurrentQueue<LogEvent>? queue;
            int remaining;
            lock (_syncRoot)
            {
                if (!_groupLogEvents.TryGetValue(jobId, out queue))
                    return;
                // Drain the accepted prefix; producers can keep emitting without blocking a flush forever.
                remaining = queue.Count;
            }
            while (remaining > 0)
            {
                var batch = new List<LogEvent>(Math.Min(_batchSizeLimit, remaining));
                while (
                    batch.Count < _batchSizeLimit
                    && batch.Count < remaining
                    && queue.TryDequeue(out var item)
                )
                    batch.Add(item);
                if (batch.Count == 0)
                    break;
                remaining -= batch.Count;
                await EmitBatchAsync(batch, jobId, title).ConfigureAwait(false);
            }
            lock (_syncRoot)
            {
                // Enqueue and removal share the lock so a producer cannot append to an orphaned queue.
                if (queue.IsEmpty)
                    _groupLogEvents.TryRemove(jobId, out _);
            }
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    public async Task FlushAllAsync(string title = "")
    {
        if (_disposed)
            return;

        var jobIds = _groupLogEvents.Keys.ToList();
        var tasks = jobIds.Select(x => FlushAsync(x, title));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    protected virtual async Task EmitBatchAsync(
        IEnumerable<LogEvent> events,
        string jobId,
        string title = ""
    )
    {
        if (_disposed)
        {
            return;
        }

        if (_sendBatchesAsOneMessages)
        {
            var sb = new StringBuilder();
            foreach (var logEvent in events)
            {
                string message = RenderMessage(logEvent);
                sb.Append(message);
            }
            sb.AppendLine(Environment.NewLine);

            var messageToSend = sb.ToString();
            await PushMessageAsync(messageToSend, title);
        }
        else
        {
            foreach (var logEvent in events)
            {
                var message = RenderMessage(logEvent);
                await PushMessageAsync(message);
            }
        }
    }

    protected abstract IPushService PushService { get; }

    public bool IsDisposed => _disposed;

    protected virtual async Task PushMessageAsync(string message, string title = "推送")
    {
        //SelfLog.WriteLine($"Trying to send message: '{message}'.");
        var result = await PushService.PushMessageAsync(message, title);
        SelfLog.WriteLine($"Response status: {result.StatusCode}.");
        try
        {
            var content = (await result.Content.ReadAsStringAsync())
                .Replace("{", "{{")
                .Replace("}", "}}");
            SelfLog.WriteLine($"Response content: {content}.{Environment.NewLine}");
        }
        catch (Exception e)
        {
            SelfLog.WriteLine(e.Message + Environment.NewLine);
        }
    }

    protected virtual string RenderMessage(LogEvent logEvent)
    {
        string msg = "";
        using (StringWriter stringWriter = new StringWriter())
        {
            this._formatter.Format(logEvent, (TextWriter)stringWriter);
            msg = stringWriter.ToString();
        }

        //msg = $"{GetEmoji(logEvent)} {msg}";

        if (msg.Contains("经验+") && msg.Contains("√"))
            msg = msg.Replace('√', '✔');

        return msg;

        /*
        if (logEvent.Exception == null)
        {
            return msg;
        }

        var sb = new StringBuilder();
        sb.AppendLine(msg);
        sb.AppendLine($"\n*{logEvent.Exception.Message}*\n");
        sb.AppendLine($"Message: `{logEvent.Exception.Message}`");
        sb.AppendLine($"Type: `{logEvent.Exception.GetType().Name}`\n");
        sb.AppendLine($"Stack Trace\n```{logEvent.Exception}```");

        return sb.ToString();
        */
    }

    protected virtual string GetEmoji(LogEvent log)
    {
        switch (log.Level)
        {
            case LogEventLevel.Verbose:
                return "⚡";
            case LogEventLevel.Debug:
                return "👉";
            case LogEventLevel.Information:
                return "ℹ";
            case LogEventLevel.Warning:
                return "⚠";
            case LogEventLevel.Error:
                return "❗";
            case LogEventLevel.Fatal:
                return "‼";
            default:
                return string.Empty;
        }
    }

    protected virtual string GetPushTitle(LogEvent triggerLogEvent)
    {
        var title = "推送";

        var msg = RenderMessage(triggerLogEvent).Replace(Environment.NewLine, "");
        var list = msg.Split('·').ToList();

        for (int i = 2; i < list.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(list[i]))
                title += $"-{list[i]}";
        }

        return title;
    }

    public virtual void Dispose()
    {
        bool owner;
        lock (_syncRoot)
        {
            if (_disposed)
                return;
            owner = !_disposing;
            _disposing = true;
        }
        if (!owner)
        {
            _disposeCompletion.Task.ConfigureAwait(false).GetAwaiter().GetResult();
            return;
        }
        try
        {
            // Synchronous disposal must not capture a caller's UI synchronization context.
            Task.Run(() => FlushAllAsync()).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            SelfLog.WriteLine(
                "Notification disposal flush failed ({0}).",
                exception.GetType().Name
            );
        }
        finally
        {
            lock (_syncRoot)
            {
                _disposed = true;
                _groupLogEvents.Clear();
            }
            BatchSinkManager.UnregisterSink(this);
            // The managed gate remains available for flushes that were already waiting on it.
            _disposeCompletion.TrySetResult(null);
        }
    }
}
