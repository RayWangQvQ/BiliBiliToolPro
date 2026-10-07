using System.Collections.Concurrent;

namespace Ray.Serilog.Sinks.Batched;

public class BatchSinkManager
{
    private static readonly ConcurrentDictionary<IBatchSink, bool> Sinks = new();

    internal static void RegisterSink(IBatchSink sink)
    {
        Sinks.TryAdd(sink, true);
    }

    internal static void UnregisterSink(IBatchSink sink)
    {
        Sinks.TryRemove(sink, out _);
    }

    public static async Task FlushAsync(string jobId, string title = "")
    {
        if (string.IsNullOrEmpty(jobId))
            return;

        var tasks = new List<Task>();

        foreach (var sink in Sinks.Keys)
        {
            if (!sink.IsDisposed)
            {
                tasks.Add(sink.FlushAsync(jobId, title));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    public static async Task FlushAllAsync(string title = "")
    {
        var tasks = new List<Task>();

        foreach (var sink in Sinks.Keys)
        {
            if (!sink.IsDisposed)
            {
                tasks.Add(sink.FlushAllAsync(title));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    public static int RegisteredSinkCount => Sinks.Count;
}
