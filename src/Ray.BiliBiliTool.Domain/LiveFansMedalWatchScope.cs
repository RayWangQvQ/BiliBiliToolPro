namespace Ray.BiliBiliTool.Domain;

// Only an explicit manual trigger bypasses the automatic watch window.
public sealed class LiveFansMedalWatchScope : IDisposable
{
    private static readonly AsyncLocal<bool?> Current = new();
    private readonly bool? _previous;
    private int _disposed;

    public LiveFansMedalWatchScope(bool manual)
    {
        _previous = Current.Value;
        Current.Value = manual;
    }

    public static bool IsManual => Current.Value == true;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            Current.Value = _previous;
    }
}
