using System.Collections.ObjectModel;
using System.Text;
using BlazingQuartz;
using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace Ray.BiliBiliTool.Web.Components.Pages.Schedules;

public partial class LogsDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private IDialogService DialogSvc { get; set; } = null!;

    [Inject]
    IExecutionLogService LogSvc { get; set; } = null!;

    [Inject]
    private IDbContextFactory<BiliDbContext> DbFactory { get; set; } = null!;

    [EditorRequired]
    [Parameter]
    public Key JobKey { get; set; } = null!;

    [EditorRequired]
    [Parameter]
    public Key? TriggerKey { get; set; }

    void Close() => MudDialog.Cancel();

    private List<BiliLogs> _logs = new();
    private bool _loading = true;
    private Timer? _timer;
    private CancellationTokenSource _cancellationTokenSource = new();
    private ElementReference _logContainerReference;
    private Random _rnd = new Random();
    private Dictionary<string, DateTime> _processedLogIds = new();
    private string? _fireInstanceId;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var execution = await context
            .ExecutionLogs.Where(x => x.JobName == JobKey.Name && x.TriggerName == TriggerKey.Name)
            .OrderByDescending(x => x.FireTimeUtc)
            .FirstOrDefaultAsync();
        _fireInstanceId = execution?.RunInstanceId;

        if (_fireInstanceId == null)
        {
            return;
        }

        await OnRefreshLogs();
        _timer = new Timer(
            async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await OnRefreshLogs();
                    StateHasChanged();
                });
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(3)
        );

        await base.OnInitializedAsync();
    }

    private async Task OnRefreshLogs()
    {
        _loading = true;

        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();
            _logs = await context
                .BiliLogs.Where(x => x.FireInstanceIdComputed == _fireInstanceId)
                .OrderBy(l => l.Timestamp)
                .Take(300) // 限制记录数量，避免加载过多数据
                .ToListAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            // 在生产环境中应该使用日志系统记录异常
            Console.WriteLine($"加载日志失败: {ex.Message}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
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
        _logs.Clear();
        StateHasChanged();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
