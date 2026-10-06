using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Components.Pages.Today;

[Authorize]
public partial class Today : ComponentBase, IDisposable
{
    [Inject]
    private ITodayTaskService TodayTaskService { get; set; } = null!;

    [Inject]
    private IOptionsMonitor<AutoRecoverOptions> AutoRecoverOptions { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private ElementReference _recoveryPanel;
    private bool _focusRecovery;

    private List<AccountTodayTasksDto> _accounts = [];
    private long? _selectedUserId;
    private IEnumerable<AccountTodayTasksDto> VisibleAccounts =>
        _selectedUserId == 0
            ? _accounts
            : _accounts.Where(account => account.UserId == _selectedUserId);
    private bool _loadingLocal;
    private bool _busy;
    private bool _biliChecked;
    private long _statusLoadVersion;
    private RedoRequest? _redoRequest;
    private bool _redoRunning;
    private bool _redoRefreshing;
    private string? _redoResult;
    private Severity _redoSeverity = Severity.Info;
    private string RedoPhase => _redoRefreshing ? "正在更新任务状态" : "正在补做";
    private string RedoButtonText => _redoRefreshing ? "更新中" : "补做中";
    private readonly Dictionary<string, TaskRecoveryProgress> _recoveryProgress = [];
    private long _recoveryVersion;
    private bool _disposed;
    private IReadOnlyDictionary<long, string> RecoveryAccountNames =>
        _accounts.ToDictionary(
            account => account.UserId,
            account => $"账号{account.Index + 1} · {account.UserName}"
        );

    private async Task ReceiveRecoveryProgress(long version, TaskRecoveryProgress update)
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (_disposed || version != _recoveryVersion || !_redoRunning)
                    return;
                if (_recoveryProgress.TryGetValue(update.Key, out var previous))
                {
                    if (
                        previous.State == TaskRecoveryProgressState.Failed
                        && update.State == TaskRecoveryProgressState.Completed
                    )
                        return;
                    update = update with
                    {
                        Current = update.Current ?? previous.Current,
                        Total = update.Total ?? previous.Total,
                        Unit = update.Unit ?? previous.Unit,
                        PlatformProgress = update.PlatformProgress ?? previous.PlatformProgress,
                    };
                }
                _recoveryProgress[update.Key] = update;
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) when (_disposed) { }
    }

    public void Dispose()
    {
        _disposed = true;
        _recoveryVersion++;
    }

    private sealed record RedoRequest(long? UserId, string? TaskKey, string? ItemKey, string Label);

    private bool MatchesRedoRow(long userId, string taskKey, string? itemKey) =>
        _redoRequest is { TaskKey: not null } request
        && request.UserId == userId
        && request.TaskKey == taskKey
        && request.ItemKey == itemKey;

    private bool MatchesRedoAccount(long userId) =>
        _redoRequest is { TaskKey: null } request
        && (request.UserId is null || request.UserId == userId);

    private bool RedoAccountRunning(long userId) => _redoRunning && MatchesRedoAccount(userId);

    private bool RedoVisibleRunning =>
        _redoRunning
        && _redoRequest is { TaskKey: null } request
        && (_selectedUserId == 0 ? request.UserId is null : request.UserId == _selectedUserId);

    private DateTimeOffset? _lastRefresh;

    private bool _autoEnable;
    private int _intervalHours = 2;
    private int _retentionDays = 3;
    private (bool Enabled, int Interval, int Retention) _savedSettings;
    private bool _savingSettings;
    private bool _settingsSaved;
    private string? _settingsError;

    private void RecoverySettingsChanged() => _settingsError = null;

    private bool SettingsChanged => (_autoEnable, _intervalHours, _retentionDays) != _savedSettings;

    protected override void OnInitialized()
    {
        LoadSettings();
    }

    /// <summary>
    /// 首屏：只用本地数据（配置 / 数据库 / Quartz）渲染，不发网络请求，秒开。
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        _loadingLocal = true;
        try
        {
            SetAccounts(await TodayTaskService.GetTodayStatusAsync(includeBili: false));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"加载今日任务失败：{ex.Message}", Severity.Error);
        }
        finally
        {
            _loadingLocal = false;
            _lastRefresh = DateTimeOffset.Now;
        }
    }

    /// <summary>
    /// 首帧渲染完成后再去并发查 B 站，页面不会卡在空白等待。
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusRecovery)
        {
            _focusRecovery = false;
            try
            {
                await JS.InvokeVoidAsync("biliTool.focusRecoveryProgress", _recoveryPanel);
            }
            catch (JSException) { }
            catch (InvalidOperationException) when (_disposed) { }
        }
        if (firstRender)
            await LoadBiliStatusAsync(force: false);
    }

    private void LoadSettings()
    {
        var config = AutoRecoverOptions.CurrentValue;
        _autoEnable = config.IsEnable;
        _intervalHours = Math.Clamp(config.IntervalHours, 1, 24);
        _retentionDays = Math.Clamp(config.RecordRetentionDays, 1, 90);
        _savedSettings = (_autoEnable, _intervalHours, _retentionDays);
    }

    private void SetAccounts(List<AccountTodayTasksDto> accounts)
    {
        _accounts = accounts;
        // Keep the selection stable across refreshes and account reordering.
        if (_selectedUserId != 0 && !_accounts.Any(account => account.UserId == _selectedUserId))
            _selectedUserId = _accounts.FirstOrDefault()?.UserId;
    }

    private void ChangeAccount(long userId)
    {
        if (!_busy && (userId == 0 || _accounts.Any(account => account.UserId == userId)))
            _selectedUserId = userId;
    }

    private Task RedoVisibleAsync() =>
        _selectedUserId == 0 ? RedoAllAsync()
        : VisibleAccounts.FirstOrDefault() is { } account ? RedoAccountAsync(account)
        : Task.CompletedTask;

    private void StepAccount(int direction)
    {
        if (_busy || _accounts.Count < 2)
            return;
        var index = _accounts.FindIndex(account => account.UserId == _selectedUserId);
        var nextIndex =
            index < 0
                ? direction > 0
                    ? 0
                    : _accounts.Count - 1
                : (index + direction + _accounts.Count) % _accounts.Count;
        _selectedUserId = _accounts[nextIndex].UserId;
    }

    /// <summary>并发查 B 站，把每日任务四项的真实状态补齐</summary>
    private async Task<bool> LoadBiliStatusAsync(bool force)
    {
        var version = ++_statusLoadVersion;
        try
        {
            var accounts = await TodayTaskService.GetTodayStatusAsync(
                includeBili: true,
                forceRefresh: force
            );
            if (version != _statusLoadVersion)
                return false;
            SetAccounts(accounts);
            _biliChecked = true;
            _lastRefresh = DateTimeOffset.Now;
            return true;
        }
        catch
        {
            if (version == _statusLoadVersion)
                Snackbar.Add("任务状态更新未完成，请点击「立即刷新」重试", Severity.Warning);
            return false;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task ManualRefreshAsync()
    {
        if (_busy)
            return;
        _busy = true;
        try
        {
            await LoadBiliStatusAsync(force: true);
        }
        finally
        {
            _busy = false;
        }
    }

    private Task RedoItemAsync(
        AccountTodayTasksDto account,
        string taskKey,
        TodayTaskItemDto item
    ) =>
        RunRedoAsync(
            new(
                account.UserId,
                taskKey,
                item.ItemKey,
                $"账号{account.Index + 1} · {account.UserName} · {item.DisplayName}"
            ),
            () => TodayTaskService.RedoAsync(account.UserId, taskKey, item.ItemKey)
        );

    private Task RedoAccountAsync(AccountTodayTasksDto account) =>
        RunRedoAsync(
            new(account.UserId, null, null, $"账号{account.Index + 1} · {account.UserName}"),
            async () =>
            {
                var count = await TodayTaskService.RedoAllForAccountAsync(account.UserId);
                return BatchRedoResult(count);
            }
        );

    private Task RedoAllAsync() =>
        RunRedoAsync(
            new(null, null, null, "全部账号"),
            async () =>
            {
                var count = await TodayTaskService.RedoAllMissingAsync();
                return BatchRedoResult(count);
            }
        );

    private static TaskRedoResultDto BatchRedoResult(int count) =>
        new(
            true,
            count == 0
                ? "当前没有需要补做的任务"
                : $"本轮补做已结束，共执行 {count} 项。各项结果见任务状态"
        );

    private async Task RunRedoAsync(RedoRequest request, Func<Task<TaskRedoResultDto>> execute)
    {
        if (_busy)
            return;
        _busy = true;
        _redoRequest = request;
        _redoRunning = true;
        _redoRefreshing = false;
        _redoResult = null;
        _redoSeverity = Severity.Info;
        _recoveryProgress.Clear();
        _focusRecovery = true;
        var version = ++_recoveryVersion;
        using var progressScope = new TaskRecoveryProgressScope(update =>
        {
            _ = ReceiveRecoveryProgress(version, update);
        });
        StateHasChanged();
        TaskRedoResultDto result;
        try
        {
            try
            {
                result = await execute();
                _redoSeverity =
                    result.Skipped ? Severity.Info
                    : result.Success
                        ? request.TaskKey is null ? Severity.Info
                            : Severity.Success
                    : Severity.Warning;
            }
            catch (Exception error)
            {
                result = new(false, TaskRecoveryProgressScope.DescribeFailure(error));
                _redoSeverity = Severity.Error;
            }
            foreach (var entry in _recoveryProgress.Values.ToArray())
                if (
                    entry.State
                    is TaskRecoveryProgressState.Running
                        or TaskRecoveryProgressState.Waiting
                )
                    _recoveryProgress[entry.Key] = entry with
                    {
                        State =
                            result.Skipped ? TaskRecoveryProgressState.Skipped
                            : result.Success ? TaskRecoveryProgressState.Completed
                            : TaskRecoveryProgressState.Pending,
                        Detail = result.Success ? "本轮执行已结束" : result.Message,
                    };
            if (
                _recoveryProgress.Values.Any(entry =>
                    entry.State == TaskRecoveryProgressState.Failed
                )
            )
                _redoSeverity = Severity.Warning;
            _redoRefreshing = true;
            StateHasChanged();
            var refreshed = await LoadBiliStatusAsync(force: true);
            _redoResult = refreshed
                ? result.Message
                : result.Message + "。任务状态更新未完成，请点击「立即刷新」查看";
            if (!refreshed && _redoSeverity is Severity.Success or Severity.Info)
                _redoSeverity = Severity.Warning;
            Snackbar.Add(_redoResult, _redoSeverity);
        }
        finally
        {
            _redoRunning = false;
            _redoRefreshing = false;
            _busy = false;
        }
    }

    /// <summary>
    /// 关闭分享任务。这是全局开关（对所有账号生效），所以先弹确认，
    /// 并且不挂在单个任务行上——放在设置区才对得上它的作用范围。
    /// </summary>
    private async Task DisableShareAsync()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "不再尝试分享",
            "这会关闭所有账号的分享任务，之后不再自动尝试分享。确定吗？",
            "确定关闭",
            "取消"
        );
        if (confirmed != true)
        {
            return;
        }

        _busy = true;
        try
        {
            await TodayTaskService.DisableShareAsync();
            Snackbar.Add("已关闭分享任务，之后不再尝试分享", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"关闭失败：{ex.Message}", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await ManualRefreshAsync();
    }

    private async Task SaveSettingsAsync()
    {
        if (_busy || _savingSettings || !SettingsChanged)
            return;
        _busy = true;
        _savingSettings = true;
        _settingsSaved = false;
        _settingsError = null;
        _intervalHours = Math.Clamp(_intervalHours, 1, 24);
        _retentionDays = Math.Clamp(_retentionDays, 1, 90);
        var submitted = (_autoEnable, _intervalHours, _retentionDays);
        StateHasChanged();
        try
        {
            await TodayTaskService.SaveAutoRecoverSettingsAsync(
                submitted._autoEnable,
                submitted._intervalHours,
                submitted._retentionDays
            );
            _savedSettings = submitted;
            _settingsSaved = true;
            Snackbar.Add("设置已保存", Severity.Success);
        }
        catch
        {
            _settingsError = "保存失败，修改已保留，请重试";
        }
        finally
        {
            _savingSettings = false;
            _busy = false;
            StateHasChanged();
        }
    }

    private async Task<bool> SaveBeforeLeavingAsync()
    {
        await SaveSettingsAsync();
        return _settingsSaved && !SettingsChanged;
    }

    /// <summary>是否有任何一项提供了「不再尝试分享」的入口（分享任务仍开启时才会有）</summary>
    private bool AnyShareDisable =>
        _accounts.SelectMany(a => a.Groups).SelectMany(g => g.Items).Any(i => i.CanDisableShare);

    /// <summary>
    /// 整任务算一项的任务（<see cref="TodayTaskItemDto.ItemKey"/> 为 null，如「充电」「批量取关」）：
    /// 任务名与检查项名完全相同，页面上不再拆成两级，直接渲染成一行。
    /// </summary>
    private static bool IsSingleItemTask(TodayTaskGroupDto group) =>
        group.Items.Count == 1 && group.Items[0].ItemKey is null;

    /// <summary>
    /// 账号级进度。「本日无需执行」与「已关闭」既不算完成也不算待办，直接排除在分母外。
    /// </summary>
    private static (int Done, int Total) ProgressOf(AccountTodayTasksDto account) =>
        ProgressOf(account.Groups.SelectMany(g => g.Items));

    /// <summary>任务级进度，口径同上。</summary>
    private static (int Done, int Total) ProgressOf(IEnumerable<TodayTaskItemDto> items)
    {
        var tracked = items
            .Where(i =>
                i.State
                    is not (
                        TodayTaskItemState.NotToday
                        or TodayTaskItemState.Disabled
                        or TodayTaskItemState.NoWork
                    )
            )
            .ToList();

        return (tracked.Count(i => i.State == TodayTaskItemState.Completed), tracked.Count);
    }

    private static string StateIcon(TodayTaskItemDto item) =>
        item.IsBiliPending ? Icons.Material.Filled.HourglassEmpty : StateIcon(item.State);

    private static string StateIcon(TodayTaskItemState state) =>
        state switch
        {
            TodayTaskItemState.Completed => Icons.Material.Filled.CheckCircle,
            TodayTaskItemState.NotDone => Icons.Material.Filled.Cancel,
            TodayTaskItemState.Failed => Icons.Material.Filled.ErrorOutline,
            TodayTaskItemState.RetryExhausted => Icons.Material.Filled.WarningAmber,
            TodayTaskItemState.Waiting
            or TodayTaskItemState.WaitingConditions
            or TodayTaskItemState.WaitingWatchTime => Icons.Material.Filled.Schedule,
            TodayTaskItemState.NoWork => Icons.Material.Filled.RemoveCircleOutline,
            TodayTaskItemState.NotToday => Icons.Material.Filled.RemoveCircleOutline,
            TodayTaskItemState.Disabled => Icons.Material.Filled.Block,
            TodayTaskItemState.Unknown => Icons.Material.Filled.HelpOutline,
            _ => Icons.Material.Filled.HelpOutline,
        };

    private static Color StateColor(TodayTaskItemDto item) =>
        item.IsBiliPending ? Color.Default : StateColor(item.State);

    private static Color StateColor(TodayTaskItemState state) =>
        state switch
        {
            TodayTaskItemState.Completed => Color.Success,
            TodayTaskItemState.NotDone => Color.Error,
            TodayTaskItemState.Failed or TodayTaskItemState.RetryExhausted => Color.Warning,
            TodayTaskItemState.Waiting
            or TodayTaskItemState.WaitingConditions
            or TodayTaskItemState.WaitingWatchTime => Color.Info,
            TodayTaskItemState.Unknown => Color.Error,
            _ => Color.Default,
        };
}
