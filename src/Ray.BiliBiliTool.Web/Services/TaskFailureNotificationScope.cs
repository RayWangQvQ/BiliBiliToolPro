namespace Ray.BiliBiliTool.Web.Services;

/// <summary>Controls failure reminders for the current asynchronous execution only.</summary>
public sealed class TaskFailureNotificationScope : IDisposable
{
    private static readonly AsyncLocal<bool> Suppressed = new();
    private readonly bool _previous;
    private bool _disposed;

    public static bool IsSuppressed => Suppressed.Value;

    public TaskFailureNotificationScope(bool suppress)
    {
        _previous = Suppressed.Value;
        Suppressed.Value = _previous || suppress;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Suppressed.Value = _previous;
    }
}
