using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MudBlazor;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Components.Pages.Today;

[Authorize]
public partial class Today : ComponentBase
{
    [Inject]
    private ITodayTaskService TodayTaskService { get; set; } = null!;

    [Inject]
    private IOptionsMonitor<AutoRecoverOptions> AutoRecoverOptions { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private List<AccountTodayTasksDto> _accounts = [];
    private bool _loadingLocal;
    private bool _busy;
    private bool _biliChecked;
    private DateTimeOffset? _lastRefresh;

    private bool _autoEnable;
    private int _intervalHours = 2;
    private int _retentionDays = 3;

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
            _accounts = await TodayTaskService.GetTodayStatusAsync(includeBili: false);
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
        if (!firstRender)
        {
            return;
        }

        await LoadBiliStatusAsync(force: false);
    }

    private void LoadSettings()
    {
        var config = AutoRecoverOptions.CurrentValue;
        _autoEnable = config.IsEnable;
        _intervalHours = Math.Clamp(config.IntervalHours, 1, 24);
        _retentionDays = Math.Clamp(config.RecordRetentionDays, 1, 90);
    }

    /// <summary>并发查 B 站，把每日任务四项的真实状态补齐</summary>
    private async Task LoadBiliStatusAsync(bool force)
    {
        try
        {
            _accounts = await TodayTaskService.GetTodayStatusAsync(
                includeBili: true,
                forceRefresh: force
            );
            _biliChecked = true;
            _lastRefresh = DateTimeOffset.Now;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"查询 B 站状态失败：{ex.Message}", Severity.Warning);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task ManualRefreshAsync()
    {
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

    private async Task RedoItemAsync(
        AccountTodayTasksDto account,
        string taskKey,
        TodayTaskItemDto item
    )
    {
        _busy = true;
        try
        {
            var result = await TodayTaskService.RedoAsync(account.UserId, taskKey, item.ItemKey);
            Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Warning);
        }
        finally
        {
            _busy = false;
        }

        await ManualRefreshAsync();
    }

    private async Task RedoAccountAsync(AccountTodayTasksDto account)
    {
        _busy = true;
        try
        {
            var count = await TodayTaskService.RedoAllForAccountAsync(account.UserId);
            Snackbar.Add($"已补做 {count} 项", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"补做失败：{ex.Message}", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await ManualRefreshAsync();
    }

    private async Task RedoAllAsync()
    {
        _busy = true;
        try
        {
            var count = await TodayTaskService.RedoAllMissingAsync();
            Snackbar.Add($"已补做 {count} 项", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"补做失败：{ex.Message}", Severity.Error);
        }
        finally
        {
            _busy = false;
        }

        await ManualRefreshAsync();
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
        _busy = true;
        try
        {
            await TodayTaskService.SaveAutoRecoverSettingsAsync(
                _autoEnable,
                _intervalHours,
                _retentionDays
            );
            Snackbar.Add("设置已保存", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"保存失败：{ex.Message}", Severity.Error);
        }
        finally
        {
            _busy = false;
        }
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
            .Where(i => i.State is not (TodayTaskItemState.NotToday or TodayTaskItemState.Disabled))
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
            TodayTaskItemState.Waiting => Icons.Material.Filled.Schedule,
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
            TodayTaskItemState.Waiting => Color.Info,
            TodayTaskItemState.Unknown => Color.Error,
            _ => Color.Default,
        };
}
