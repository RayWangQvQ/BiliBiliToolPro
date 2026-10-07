using BlazingQuartz.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;

namespace Ray.BiliBiliTool.Web.Components.Pages.Schedules;

public partial class LogsDialog : ComponentBase, IDisposable
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private IDialogService DialogSvc { get; set; } = null!;

    [Inject]
    private ILogsDialogWorkflow LogsWorkflow { get; set; } = null!;

    [Inject]
    private IServiceProvider ComponentServices { get; set; } = null!;

    [Inject]
    private ILogger<LogsDialog> Logger { get; set; } = NullLogger<LogsDialog>.Instance;

    [EditorRequired]
    [Parameter]
    public Key JobKey { get; set; } = null!;

    [EditorRequired]
    [Parameter]
    public Key? TriggerKey { get; set; }

    void Close() => MudDialog.Cancel();

    private List<BiliLogs> _logs = new();
    private bool _loading = true;
    private readonly object _lifecycleLock = new();
    private ITimer? _timer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _refreshCancellation;
    private int _disposed;
    private int _refreshing;
    private ElementReference _logContainerReference;
    private string? _fireInstanceId;

    public LogsDialog()
    {
        _refreshCancellation = _cancellationTokenSource.Token;
    }

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    protected override async Task OnInitializedAsync()
    {
        if (IsDisposed)
            return;

        try
        {
            _fireInstanceId = await LogsWorkflow.GetLatestRunInstanceIdAsync(
                JobKey.Name,
                TriggerKey?.Name,
                _refreshCancellation
            );
        }
        catch (OperationCanceledException) when (_refreshCancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
            {
                _loading = false;
                Logger.LogWarning("加载任务日志失败（{exceptionType}）", exception.GetType().Name);
            }
            return;
        }

        if (IsDisposed)
            return;

        if (string.IsNullOrWhiteSpace(_fireInstanceId))
        {
            _loading = false;
            return;
        }

        await OnRefreshLogs();
        lock (_lifecycleLock)
        {
            if (!IsDisposed)
            {
                var clock = ComponentServices.GetService<TimeProvider>() ?? TimeProvider.System;
                _timer = clock.CreateTimer(
                    OnTimerTick,
                    null,
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(3)
                );
            }
        }

        await base.OnInitializedAsync();
    }

    private async Task OnRefreshLogs()
    {
        if (
            IsDisposed
            || string.IsNullOrWhiteSpace(_fireInstanceId)
            || Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0
        )
            return;

        _loading = true;

        try
        {
            var logs = await LogsWorkflow.GetLogsForRunAsync(
                _fireInstanceId,
                300,
                _refreshCancellation
            );
            if (!IsDisposed)
                _logs = logs;
        }
        catch (OperationCanceledException) when (_refreshCancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!IsDisposed)
                Logger.LogWarning("加载任务日志失败（{exceptionType}）", exception.GetType().Name);
        }
        finally
        {
            Volatile.Write(ref _refreshing, 0);
            if (!IsDisposed)
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }

    private void OnTimerTick(object? state)
    {
        if (!IsDisposed)
            _ = RefreshFromTimerAsync();
    }

    private async Task RefreshFromTimerAsync()
    {
        try
        {
            await InvokeAsync(OnRefreshLogs);
        }
        catch (OperationCanceledException) when (_refreshCancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!IsDisposed)
                Logger.LogWarning("日志自动刷新失败（{exceptionType}）", exception.GetType().Name);
        }
    }

    private string GetLogLevelClass(string logLevel)
    {
        return logLevel.ToLower() switch
        {
            "error" => "log-level-error",
            "warning" => "log-level-warning",
            "debug" => "log-level-debug",
            _ => "log-level-info",
        };
    }

    private void ClearDisplay()
    {
        if (IsDisposed)
            return;
        _logs.Clear();
        StateHasChanged();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        lock (_lifecycleLock)
        {
            _timer?.Dispose();
            _timer = null;
        }
        try
        {
            _cancellationTokenSource.Cancel();
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
