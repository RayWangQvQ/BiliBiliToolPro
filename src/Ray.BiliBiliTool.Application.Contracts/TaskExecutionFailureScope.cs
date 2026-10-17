namespace Ray.BiliBiliTool.Application.Contracts;

// Intercepted failures must survive exception suppression without retaining exception details.
public sealed class TaskExecutionFailureScope : IDisposable
{
    private static readonly AsyncLocal<TaskExecutionFailureScope?> Current = new();
    private readonly TaskExecutionFailureScope? _parent = Current.Value;
    private int _failureCount;

    public TaskExecutionFailureScope() => Current.Value = this;

    public bool HasFailures => Volatile.Read(ref _failureCount) > 0;

    public static void MarkFailed()
    {
        if (Current.Value is { } scope)
            Interlocked.Increment(ref scope._failureCount);
    }

    public void Dispose() => Current.Value = _parent;
}
