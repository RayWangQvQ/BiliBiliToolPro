using System.Collections.ObjectModel;
using System.Text;
using BlazingQuartz;
using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor;
using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Components.Pages.Schedules;

public partial class HistoryDialog : ComponentBase
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private IDialogService DialogSvc { get; set; } = null!;

    [Inject]
    IExecutionLogService LogSvc { get; set; } = null!;

    [EditorRequired]
    [Parameter]
    public Key JobKey { get; set; } = null!;

    [EditorRequired]
    [Parameter]
    public Key? TriggerKey { get; set; }

    ObservableCollection<ExecutionLog> ExecutionLogs { get; set; } = new();
    bool HasMore { get; set; } = false;

    private PageMetadata? _lastPageMeta;
    private long _firstLogId;

    void Close() => MudDialog.Cancel();

    protected override async Task OnInitializedAsync()
    {
        await OnRefreshHistory();
    }

    private async Task GetMoreLogs()
    {
        PageMetadata pageMeta;
        if (_lastPageMeta == null)
        {
            pageMeta = new(0, 5);
        }
        else
        {
            pageMeta = _lastPageMeta with { Page = _lastPageMeta.Page + 1 };
        }

        var result = await LogSvc.GetLatestExecutionLog(
            JobKey.Name,
            JobKey.Group ?? BlazingQuartz.Constants.DEFAULT_GROUP,
            TriggerKey?.Name,
            TriggerKey?.Group,
            pageMeta,
            _firstLogId
        );

        _lastPageMeta = result.PageMetadata;
        if (pageMeta.Page == 0)
        {
            _firstLogId = result.FirstOrDefault()?.LogId ?? 0;
        }

        result.ForEach(l => ExecutionLogs.Add(l));

        HasMore = result.Count == pageMeta.PageSize;
    }

    private async Task OnRefreshHistory()
    {
        ExecutionLogs.Clear();
        _lastPageMeta = null;
        _firstLogId = 0;
        HasMore = false;

        await GetMoreLogs();
    }

    private void OnMoreDetails(ExecutionLog log, string title)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
        };

        var parameters = new DialogParameters { ["ExecutionLog"] = log };
        // var dlg = DialogSvc.Show<ExecutionDetailsDialog>(title, parameters, options);
    }

    private string GetExecutionTime(ExecutionLog log)
    {
        // when fire time is available, display time range
        // otherwise just display date added
        if (log.FireTimeUtc.HasValue)
        {
            StringBuilder strBldr = new(
                log.FireTimeUtc.Value.LocalDateTime.ToShortDateString()
                    + " "
                    + log.FireTimeUtc.Value.LocalDateTime.ToLongTimeString()
            );

            var finishTime = log.GetFinishTimeUtc();
            if (finishTime.HasValue)
            {
                strBldr.Append(" - ");
                if (finishTime.Value.LocalDateTime.Date != log.FireTimeUtc.Value.LocalDateTime.Date)
                {
                    // display ending date
                    strBldr.Append(finishTime.Value.LocalDateTime.ToShortDateString() + " ");
                }

                strBldr.Append(finishTime.Value.LocalDateTime.ToLongTimeString());
            }
            return strBldr.ToString();
        }
        else
        {
            return log.DateAddedUtc.LocalDateTime.ToShortDateString()
                + " "
                + log.DateAddedUtc.LocalDateTime.ToLongTimeString();
        }
    }

    private Color GetTimelineDotColor(ExecutionLog log)
    {
        return log.LogType switch
        {
            LogType.ScheduleJob => ((log.IsException ?? false) ? Color.Error : Color.Success),
            _ => Color.Tertiary,
        };
    }
}
