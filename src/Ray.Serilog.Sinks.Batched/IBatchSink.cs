using Serilog.Events;

namespace Ray.Serilog.Sinks.Batched;

public interface IBatchSink
{
    void Emit(LogEvent logEvent);

    Task FlushAsync(string jobId, string title = "");

    Task FlushAllAsync(string title = "");

    bool IsDisposed { get; }
}
