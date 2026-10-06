using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public partial class LiveFansMedalTaskConfig
    : BaseConfigComponent<LiveFansMedalTaskOptions>,
        IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();

    [Inject]
    private ILiveMedalDashboardService Dashboard { get; set; } = null!;
    private IReadOnlyList<LiveMedalAccount> _accounts = [];
    private int _accountIndex;
    private LiveMedalSnapshot? _snapshot;
    private bool _medalsLoading;
    private string? _medalRefreshError;
    private CancellationTokenSource? _accountLifetime;
    private IDisposable? _progressSubscription;
    private long _subscriptionGeneration;
    private long _loadVersion;
    private bool _backgroundRefreshing;

    [Inject]
    private IDialogService Dialogs { get; set; } = null!;

    [Inject]
    private ILiveMedalParticipationWorkflow Participation { get; set; } = null!;
    private bool _selectionBusy;
    private bool _selectionSaving;
    private string? _selectionMessage;
    private bool _selectionFailed;

    private void SyncExclusionSetting()
    {
        var saved = OptionsMonitor.CurrentValue.ExcludedAnchorIds;
        _config.ExcludedAnchorIds = saved;
        UpdateSavedSetting(LiveMedalParticipationWorkflow.ExclusionKey, saved);
    }

    protected override Dictionary<string, string> ValuesToPersist(
        LiveFansMedalTaskOptions submitted
    )
    {
        var values = submitted.ToConfigDictionary();
        values.Remove(LiveMedalParticipationWorkflow.ExclusionKey);
        return values;
    }

    protected override async Task HandleValidSubmitAsync()
    {
        if (_selectionBusy)
            return;
        SyncExclusionSetting();
        await base.HandleValidSubmitAsync();
        SyncExclusionSetting();
    }

    private async Task ChangeExclusionAsync(LiveMedalExclusionChange change)
    {
        if (_selectionBusy || _isSaving || _lifetime.IsCancellationRequested)
            return;
        var name =
            _snapshot?.Medals.FirstOrDefault(card => card.AnchorId == change.AnchorId)?.AnchorName
            ?? "这位主播";
        _selectionBusy = true;
        _selectionFailed = false;
        _selectionMessage = null;
        StateHasChanged();
        try
        {
            var confirmed = await Dialogs.ShowMessageBoxAsync(
                change.Excluded ? "排除此主播" : "恢复主播参与",
                change.Excluded
                    ? $"确认排除「{name}」？该主播将停止参与自动任务，并移到列表末尾。设置会立即保存。"
                    : $"确认恢复「{name}」参与任务？设置会立即保存。",
                yesText: change.Excluded ? "确定排除" : "恢复参与",
                cancelText: "取消"
            );
            if (confirmed != true || _lifetime.IsCancellationRequested)
                return;
            _selectionSaving = true;
            _selectionMessage = "正在保存主播参与设置…";
            StateHasChanged();
            var saved = await Participation.SetExcludedAsync(
                change.AnchorId,
                change.Excluded,
                _lifetime.Token
            );
            if (_lifetime.IsCancellationRequested)
                return;
            _config.ExcludedAnchorIds = saved;
            UpdateSavedSetting(LiveMedalParticipationWorkflow.ExclusionKey, saved);
            _selectionMessage = change.Excluded
                ? $"已排除「{name}」，设置已保存"
                : $"已恢复「{name}」参与任务，设置已保存";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            Logger.LogWarning("Medal participation save failed: {ErrorType}", error.GetType().Name);
            _selectionFailed = true;
            _selectionMessage = "主播参与设置保存失败，请重试";
        }
        finally
        {
            _selectionBusy = false;
            _selectionSaving = false;
            if (!_lifetime.IsCancellationRequested)
                StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _accounts = Dashboard.GetAccounts();
        SubscribeToAccount();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _accounts.Count > 0)
        {
            await LoadMedalsAsync();
            if (!_lifetime.IsCancellationRequested)
                _ = AutoRefreshAsync();
        }
    }

    private int LikeLimit
    {
        get => _config.UseLiveStateMonitoring ? _config.DailyLikeNumber : _config.LikeNumber;
        set
        {
            if (_config.UseLiveStateMonitoring)
                _config.DailyLikeNumber = value;
            else
                _config.LikeNumber = value;
        }
    }

    private int DanmakuLimit
    {
        get =>
            _config.UseLiveStateMonitoring ? _config.DailyDanmakuNumber : _config.SendDanmakuNumber;
        set
        {
            if (_config.UseLiveStateMonitoring)
                _config.DailyDanmakuNumber = value;
            else
                _config.SendDanmakuNumber = value;
        }
    }

    private int WatchLimit
    {
        get => _config.UseLiveStateMonitoring ? _config.DailyWatchMinutes : _config.HeartBeatNumber;
        set
        {
            if (_config.UseLiveStateMonitoring)
                _config.DailyWatchMinutes = value;
            else
                _config.HeartBeatNumber = value;
        }
    }

    private Task ReloadAsync() => LoadConfigAsync();

    private Task RefreshMedalsAsync() => LoadMedalsAsync();

    private void SubscribeToAccount()
    {
        _progressSubscription?.Dispose();
        _accountLifetime?.Cancel();
        _accountLifetime?.Dispose();
        _accountLifetime = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var generation = ++_subscriptionGeneration;
        _progressSubscription =
            _accounts.Count == 0
                ? null
                : Dashboard.Subscribe(
                    _accountIndex,
                    snapshot => _ = ReceiveProgressAsync(snapshot, generation)
                );
    }

    private async Task ReceiveProgressAsync(LiveMedalSnapshot snapshot, long generation)
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (_lifetime.IsCancellationRequested || generation != _subscriptionGeneration)
                    return;
                AcceptSnapshot(snapshot);
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) when (_lifetime.IsCancellationRequested) { }
    }

    private void AcceptSnapshot(LiveMedalSnapshot snapshot)
    {
        if (
            _snapshot is not null
            && snapshot.Revision > 0
            && _snapshot.Revision > snapshot.Revision
        )
            return;
        if (snapshot.Error is not null && _snapshot is not null)
            _medalRefreshError = snapshot.Error;
        else
        {
            _snapshot = snapshot;
            _medalRefreshError = null;
        }
    }

    private async Task AutoRefreshAsync()
    {
        using var timer = new PeriodicTimer(Dashboard.AutoRefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(_lifetime.Token))
                await InvokeAsync(async () =>
                {
                    if (_medalsLoading || _backgroundRefreshing)
                        return;
                    if (!_selectionBusy)
                        SyncExclusionSetting();
                    _accounts = Dashboard.GetAccounts();
                    if (_accounts.Count == 0)
                        return;
                    if (!_accounts.Any(account => account.Index == _accountIndex))
                    {
                        _accountIndex = _accounts[0].Index;
                        _snapshot = null;
                    }
                    SubscribeToAccount();
                    await LoadMedalsAsync(silent: true);
                });
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (_lifetime.IsCancellationRequested) { }
        catch (InvalidOperationException) when (_lifetime.IsCancellationRequested) { }
    }

    private async Task ChangeAccountAsync(int index)
    {
        if (_medalsLoading || !_accounts.Any(account => account.Index == index))
            return;
        _accountIndex = index;
        _snapshot = null;
        _backgroundRefreshing = false;
        SubscribeToAccount();
        await LoadMedalsAsync();
    }

    private async Task LoadMedalsAsync(bool silent = false)
    {
        if (_medalsLoading || _backgroundRefreshing || _lifetime.IsCancellationRequested)
            return;
        var version = ++_loadVersion;
        var account = _accountIndex;
        var token = _accountLifetime!.Token;
        _backgroundRefreshing = silent;
        _medalsLoading = !silent;
        if (!silent)
        {
            _medalRefreshError = null;
        }
        StateHasChanged();
        try
        {
            if (_snapshot is null)
            {
                var cached = await Dashboard.GetCachedAsync(account, token);
                if (token.IsCancellationRequested || version != _loadVersion)
                    return;
                _snapshot = cached;
                if (!silent)
                    StateHasChanged();
            }
            var latest = await Dashboard.GetAsync(account, refresh: true, token: token);
            if (token.IsCancellationRequested || version != _loadVersion)
                return;
            AcceptSnapshot(latest);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch
        {
            if (version != _loadVersion || _lifetime.IsCancellationRequested)
                return;
            if (_snapshot is null)
                _snapshot = new([], DateTimeOffset.UtcNow, "任务进度暂未加载，正在等待更新");
            else
                _medalRefreshError = "进度更新未完成，稍后自动重试";
        }
        finally
        {
            if (version == _loadVersion)
            {
                _medalsLoading = false;
                _backgroundRefreshing = false;
                if (!_lifetime.IsCancellationRequested)
                    StateHasChanged();
            }
        }
    }

    public void Dispose()
    {
        ++_subscriptionGeneration;
        _progressSubscription?.Dispose();
        _accountLifetime?.Cancel();
        _accountLifetime?.Dispose();
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    [Inject]
    private IOptionsMonitor<LiveFansMedalTaskOptions> LiveFansMedalTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<LiveFansMedalTaskOptions> OptionsMonitor =>
        LiveFansMedalTaskOptionsMonitor;

    protected override bool IsScheduleEnabled(LiveFansMedalTaskOptions submitted) =>
        submitted.IsEnable && !submitted.UseLiveStateMonitoring;

    protected override JobKey GetJobKey() => LiveFansMedalJob.Key;
}
