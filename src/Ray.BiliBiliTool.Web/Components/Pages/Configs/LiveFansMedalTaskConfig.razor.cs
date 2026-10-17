using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
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

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        CloneDraft();
        _accounts = Dashboard.GetAccounts();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _accounts.Count > 0)
            await LoadMedalsAsync(false);
    }

    private void CloneDraft() =>
        _config = JsonSerializer.Deserialize<LiveFansMedalTaskOptions>(
            JsonSerializer.Serialize(_config)
        )!;

    private async Task ReloadAsync()
    {
        await LoadConfigAsync();
        CloneDraft();
    }

    private Task RefreshMedalsAsync() => LoadMedalsAsync(true);

    private async Task ChangeAccountAsync(int index)
    {
        if (_medalsLoading || !_accounts.Any(account => account.Index == index))
            return;
        _accountIndex = index;
        await LoadMedalsAsync(false);
    }

    private async Task LoadMedalsAsync(bool refresh)
    {
        if (_medalsLoading)
            return;
        _medalsLoading = true;
        _snapshot = null;
        StateHasChanged();
        try
        {
            _snapshot = await Dashboard.GetAsync(_accountIndex, refresh, _lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch
        {
            _snapshot = new([], DateTimeOffset.UtcNow, "任务进度暂未加载，请刷新");
        }
        finally
        {
            _medalsLoading = false;
            if (!_lifetime.IsCancellationRequested)
                StateHasChanged();
        }
    }

    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    [Inject]
    private IOptionsMonitor<LiveFansMedalTaskOptions> LiveFansMedalTaskOptionsMonitor { get; set; } =
        null!;

    protected override IOptionsMonitor<LiveFansMedalTaskOptions> OptionsMonitor =>
        LiveFansMedalTaskOptionsMonitor;

    protected override JobKey GetJobKey() => LiveFansMedalJob.Key;
}
