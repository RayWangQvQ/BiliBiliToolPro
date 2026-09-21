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

    private async Task DisableShareAsync()
    {
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

    private static string StateIcon(TodayTaskItemDto item) =>
        item.IsBiliPending ? "⏳" : StateIcon(item.State);

    private static string StateIcon(TodayTaskItemState state) =>
        state switch
        {
            TodayTaskItemState.Completed => "✅",
            TodayTaskItemState.NotDone => "❌",
            TodayTaskItemState.Failed => "⚠️",
            TodayTaskItemState.RetryExhausted => "⚠️",
            TodayTaskItemState.Waiting => "⏳",
            TodayTaskItemState.NotToday => "➖",
            TodayTaskItemState.Disabled => "⛔",
            TodayTaskItemState.Unknown => "❓",
            _ => "•",
        };

    private static string StateClass(TodayTaskItemDto item) =>
        item.IsBiliPending ? "state-muted" : StateClass(item.State);

    private static string StateClass(TodayTaskItemState state) =>
        state switch
        {
            TodayTaskItemState.Completed => "state-ok",
            TodayTaskItemState.NotDone => "state-bad",
            TodayTaskItemState.Failed or TodayTaskItemState.RetryExhausted => "state-warn",
            TodayTaskItemState.Unknown => "state-bad",
            _ => "state-muted",
        };
}
