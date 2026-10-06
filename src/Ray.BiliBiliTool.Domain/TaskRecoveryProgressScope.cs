using System.Text.RegularExpressions;
using Ray.BiliBiliTool.Domain.Exceptions;

namespace Ray.BiliBiliTool.Domain;

public enum TaskRecoveryProgressState
{
    Waiting,
    Running,
    Completed,
    Pending,
    Skipped,
    Failed,
}

public sealed record TaskRecoveryProgress(
    string Key,
    string Title,
    TaskRecoveryProgressState State,
    string? Detail = null,
    long? UserId = null,
    int? Current = null,
    int? Total = null,
    string? Unit = null,
    string? PlatformProgress = null
);

// Observation follows the current recovery only, including parallel medal actions.
public sealed class TaskRecoveryProgressScope : IDisposable
{
    private static readonly AsyncLocal<TaskRecoveryProgressScope?> CurrentScope = new();
    private readonly TaskRecoveryProgressScope? _previous;
    private readonly Action<TaskRecoveryProgress>? _observer;
    private readonly string _prefix;
    private readonly long? _userId;
    private readonly TaskRecoveryProgressScope _root;
    private int _active = 1;
    private int _failed;
    public bool HasFailures => Volatile.Read(ref _failed) != 0;

    public TaskRecoveryProgressScope(Action<TaskRecoveryProgress> observer)
    {
        _previous = CurrentScope.Value;
        _observer = observer;
        _prefix = "";
        _root = this;
        CurrentScope.Value = this;
    }

    public TaskRecoveryProgressScope(string prefix, long userId)
    {
        _previous = CurrentScope.Value;
        _observer = _previous?._observer;
        _root = _previous?._root ?? this;
        _prefix = prefix;
        _userId = userId;
        CurrentScope.Value = this;
    }

    public static void Report(
        string key,
        string title,
        TaskRecoveryProgressState state,
        string? detail = null,
        int? current = null,
        int? total = null,
        string? unit = null,
        string? platformProgress = null
    )
    {
        var scope = CurrentScope.Value;
        if (scope?._observer is null || Volatile.Read(ref scope._root._active) == 0)
            return;
        if (state == TaskRecoveryProgressState.Failed)
            Interlocked.Exchange(ref scope._failed, 1);
        try
        {
            scope._observer(
                new(
                    $"{scope._prefix}/{key}",
                    title,
                    state,
                    Clean(detail),
                    scope._userId,
                    current,
                    total,
                    unit,
                    Clean(platformProgress)
                )
            );
        }
        catch
        {
            // A disconnected viewer must not affect task execution.
        }
    }

    public static string DescribeFailure(Exception error) =>
        error switch
        {
            BiliBusinessException or InvalidOperationException => Clean(error.Message)
                ?? "任务执行失败",
            HttpRequestException request => request.StatusCode is { } code
                ? $"请求 B 站失败，HTTP {(int)code}"
                : "连接 B 站失败，请稍后重试",
            TaskCanceledException or TimeoutException => "请求超时，请稍后重试",
            OperationCanceledException => "补做已停止",
            IOException => "读取或保存任务数据失败，请查看执行记录",
            _ => $"执行失败（{error.GetType().Name}），请查看执行记录",
        };

    private static string? Clean(string? message)
    {
        if (message is null)
            return null;
        var text = Regex.Replace(
            message,
            @"(?i)(SESSDATA|bili_jct|sendkey|authorization|cookie)\s*[:=]\s*[^\r\n]+",
            "$1=***",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)
        );
        text = Regex.Replace(
            text,
            @"https?://\S+",
            "接口",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)
        );
        return text.Length > 300 ? text[..300] + "…" : text;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _active, 0);
        CurrentScope.Value = _previous;
    }
}
