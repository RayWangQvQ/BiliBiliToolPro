using System.Collections.ObjectModel;
using BlazingQuartz;
using BlazingQuartz.Core.Events;
using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Quartz;
using Ray.BiliBiliTool.Web.Extensions;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;

namespace Ray.BiliBiliTool.Web.Components.Pages.Schedules;

public partial class Schedules : ComponentBase, IDisposable
{
    private ScheduleJobFilter _filter = new();
    private readonly Func<ScheduleModel, object> _groupDefinition = x => x.JobGroup;
    private MudDataGrid<ScheduleModel> _scheduleDataGrid = new();

    [Inject]
    private ILogger<Schedules> Logger { get; set; } = null!;

    [Inject]
    private ISchedulerPageWorkflow SchedulerWorkflow { get; set; } = null!;

    [Inject]
    private ISchedulerListenerService SchedulerListenerSvc { get; set; } = null!;

    [Inject]
    private IDialogService DialogSvc { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private ObservableCollection<ScheduleModel> ScheduledJobs { get; } = [];

    private static Func<ScheduleModel, int, string> ScheduleRowStyleFunc =>
        (model, i) =>
        {
            if (model.JobStatus == JobStatus.NoSchedule || model.JobStatus == JobStatus.Error)
                return "background-color:var(--mud-palette-background-grey)";

            return "";
        };

    protected override async Task OnInitializedAsync()
    {
        RegisterEventListeners();
        await RefreshJobs();
    }

    public void Dispose() => UnRegisterEventListeners();

    internal bool IsRunActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule || model.JobStatus == JobStatus.NoTrigger;

    internal bool IsPauseActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule
        || model.JobStatus == JobStatus.Error
        || model.JobStatus == JobStatus.NoTrigger;

    internal bool IsTriggerNowActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule
        || model.JobStatus == JobStatus.Error
        || model.JobStatus == JobStatus.Running;

    internal bool IsHistoryActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule;

    private void RegisterEventListeners()
    {
        SchedulerListenerSvc.OnJobToBeExecuted += SchedulerListenerSvc_OnJobToBeExecuted;
        SchedulerListenerSvc.OnJobScheduled += SchedulerListenerSvc_OnJobScheduled;
        SchedulerListenerSvc.OnJobWasExecuted += SchedulerListenerSvc_OnJobWasExecuted;
        SchedulerListenerSvc.OnTriggerFinalized += SchedulerListenerSvc_OnTriggerFinalized;
        SchedulerListenerSvc.OnJobDeleted += SchedulerListenerSvc_OnJobDeleted;
        SchedulerListenerSvc.OnJobUnscheduled += SchedulerListenerSvc_OnJobUnscheduled;
        SchedulerListenerSvc.OnTriggerResumed += SchedulerListenerSvc_OnTriggerResumed;
        SchedulerListenerSvc.OnTriggerPaused += SchedulerListenerSvc_OnTriggerPaused;
    }

    private async Task RefreshJobs()
    {
        ScheduledJobs.Clear();

        IAsyncEnumerable<ScheduleModel> jobs = SchedulerWorkflow.GetAllJobsAsync(_filter);
        await foreach (ScheduleModel job in jobs)
        {
            ScheduledJobs.Add(job);
        }

        if (ScheduledJobs.Any())
            await _scheduleDataGrid.ExpandAllGroupsAsync();

        await SchedulerWorkflow.EnrichWithLastExecutionAsync(ScheduledJobs);
    }

    private void UnRegisterEventListeners()
    {
        SchedulerListenerSvc.OnJobToBeExecuted -= SchedulerListenerSvc_OnJobToBeExecuted;
        SchedulerListenerSvc.OnJobScheduled -= SchedulerListenerSvc_OnJobScheduled;
        SchedulerListenerSvc.OnJobWasExecuted -= SchedulerListenerSvc_OnJobWasExecuted;
        SchedulerListenerSvc.OnTriggerFinalized -= SchedulerListenerSvc_OnTriggerFinalized;
        SchedulerListenerSvc.OnJobDeleted -= SchedulerListenerSvc_OnJobDeleted;
        SchedulerListenerSvc.OnJobUnscheduled -= SchedulerListenerSvc_OnJobUnscheduled;
        SchedulerListenerSvc.OnTriggerResumed -= SchedulerListenerSvc_OnTriggerResumed;
        SchedulerListenerSvc.OnTriggerPaused -= SchedulerListenerSvc_OnTriggerPaused;
    }

    private async void SchedulerListenerSvc_OnTriggerPaused(object? sender, EventArgs<TriggerKey> e)
    {
        TriggerKey triggerKey = e.Args;

        await InvokeAsync(() =>
        {
            ScheduleModel? model = FindScheduleModelByTrigger(triggerKey).SingleOrDefault();
            if (model != null)
            {
                model.JobStatus = JobStatus.Paused;
                StateHasChanged();
            }
        });
    }

    private async void SchedulerListenerSvc_OnTriggerResumed(
        object? sender,
        EventArgs<TriggerKey> e
    )
    {
        TriggerKey triggerKey = e.Args;

        await InvokeAsync(() =>
        {
            ScheduleModel? model = FindScheduleModelByTrigger(triggerKey).SingleOrDefault();
            if (model != null)
            {
                model.JobStatus = JobStatus.Idle;
                StateHasChanged();
            }
        });
    }

    private async void SchedulerListenerSvc_OnJobUnscheduled(
        object? sender,
        EventArgs<TriggerKey> e
    )
    {
        Logger.LogInformation("Job trigger {triggerKey} got unscheduled", e.Args);
        await OnTriggerRemoved(e.Args);
    }

    private async void SchedulerListenerSvc_OnJobDeleted(object? sender, EventArgs<JobKey> e)
    {
        JobKey jobKey = e.Args;
        Logger.LogInformation("Delete all schedule job {jobKey}", jobKey);

        await InvokeAsync(() =>
        {
            List<ScheduleModel> modelList = ScheduledJobs
                .Where(s => s.JobName == jobKey.Name && s.JobGroup == jobKey.Group)
                .ToList();
            modelList.ForEach(s => ScheduledJobs.Remove(s));
        });
    }

    private async void SchedulerListenerSvc_OnTriggerFinalized(
        object? sender,
        EventArgs<ITrigger> e
    )
    {
        TriggerKey triggerKey = e.Args.Key;
        Logger.LogInformation("Trigger {triggerKey} finalized", triggerKey);

        await OnTriggerRemoved(triggerKey);
    }

    private async Task OnTriggerRemoved(TriggerKey triggerKey) =>
        await InvokeAsync(async () =>
        {
            ScheduleModel? model;
            try
            {
                model = FindScheduleModelByTrigger(triggerKey).SingleOrDefault();
            }
            catch (Exception ex)
            {
                Snackbar.Add(
                    $"Cannot update trigger status. Found more than one schedule with trigger {triggerKey}",
                    Severity.Warning
                );
                Logger.LogWarning(
                    ex,
                    "Cannot update trigger status. Found more than one schedule with trigger {triggerKey}",
                    triggerKey
                );
                return;
            }

            if (model is not null)
            {
                if (model.JobName == null || model.JobStatus == JobStatus.Error)
                {
                    // Just remove if no way to get job details
                    // if status is error, means get job details will throw exception
                    ScheduledJobs.Remove(model);
                }
                else
                {
                    JobDetailModel? jobDetail = await SchedulerWorkflow.GetJobDetailAsync(
                        model.JobName,
                        model.JobGroup
                    );

                    if (jobDetail != null && jobDetail.IsDurable)
                    {
                        // see if similar job name already exists
                        bool similarJobNameExists = ScheduledJobs.Any(s =>
                            s != model && s.JobName == model.JobName && s.JobGroup == model.JobGroup
                        );
                        if (similarJobNameExists)
                        {
                            // delete this duplicate no trigger job
                            ScheduledJobs.Remove(model);
                        }
                        else
                        {
                            model.JobStatus = JobStatus.NoTrigger;
                            model.ClearTrigger();
                        }
                    }
                    else
                    {
                        model.JobStatus = JobStatus.NoSchedule;
                    }
                }

                StateHasChanged();
            }
        });

    private async void SchedulerListenerSvc_OnJobWasExecuted(
        object? sender,
        JobWasExecutedEventArgs e
    )
    {
        JobKey jobKey = e.JobExecutionContext.JobDetail.Key;
        TriggerKey triggerKey = e.JobExecutionContext.Trigger.Key;

        await InvokeAsync(() =>
        {
            ScheduleModel? model = FindScheduleModel(jobKey, triggerKey).SingleOrDefault();
            if (model is not null)
            {
                model.PreviousTriggerTime = e.JobExecutionContext.FireTimeUtc;
                model.NextTriggerTime = e.JobExecutionContext.NextFireTimeUtc;
                model.JobStatus = JobStatus.Idle;
                bool? isSuccess = e.JobExecutionContext.GetIsSuccess();
                if (e.JobException != null)
                    model.ExceptionMessage = e.JobException.Message;
                else if (isSuccess.HasValue && !isSuccess.Value)
                    model.ExceptionMessage = e.JobExecutionContext.GetReturnCodeAndResult();

                StateHasChanged();
            }
        });
    }

    private async void SchedulerListenerSvc_OnJobScheduled(object? sender, EventArgs<ITrigger> e)
    {
        if (
            !_filter.IncludeSystemJobs
            && (
                e.Args.JobKey.Group == BlazingQuartz.Constants.SYSTEM_GROUP
                || e.Args.Key.Group == BlazingQuartz.Constants.SYSTEM_GROUP
            )
        )
        {
            // system job is not visible, skip this event
            return;
        }

        await InvokeAsync(async () =>
        {
            ScheduleModel model = await SchedulerWorkflow.GetScheduleModelAsync(e.Args);
            ScheduledJobs.Add(model);
        });
    }

    private async void SchedulerListenerSvc_OnJobToBeExecuted(
        object? sender,
        EventArgs<IJobExecutionContext> e
    )
    {
        JobKey jobKey = e.Args.JobDetail.Key;
        TriggerKey triggerKey = e.Args.Trigger.Key;

        await InvokeAsync(() =>
        {
            ScheduleModel? model = FindScheduleModel(jobKey, triggerKey).SingleOrDefault();
            if (model is not null)
            {
                model.JobStatus = JobStatus.Running;

                StateHasChanged();
            }
        });
    }

    private IEnumerable<ScheduleModel> FindScheduleModelByTrigger(TriggerKey triggerKey) =>
        ScheduledJobs.Where(j =>
            j.EqualsTriggerKey(triggerKey)
            && j.JobStatus != JobStatus.NoSchedule
            && j.JobStatus != JobStatus.NoTrigger
        );

    private IEnumerable<ScheduleModel> FindScheduleModel(JobKey jobKey, TriggerKey? triggerKey) =>
        ScheduledJobs.Where(j =>
            j.Equals(jobKey, triggerKey)
            && (
                (j.JobStatus != JobStatus.NoSchedule && j.JobStatus != JobStatus.NoTrigger)
                || (j.JobStatus == JobStatus.Error && j.TriggerName != null)
            )
        );

    private async Task OnResumeScheduleJob(ScheduleModel model)
    {
        var result = await SchedulerWorkflow.ResumeJobAsync(model);
        if (!result.IsSuccess)
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
    }

    private async Task OnPauseScheduleJob(ScheduleModel model)
    {
        var result = await SchedulerWorkflow.PauseJobAsync(model);
        if (!result.IsSuccess)
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
    }

    private void OnJobHistory(ScheduleModel model)
    {
        if (model.JobName == null)
        {
            // not possible?
            return;
        }

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
        };

        var parameters = new DialogParameters
        {
            ["JobKey"] = new Key(model.JobName, model.JobGroup),
            ["TriggerKey"] =
                model.TriggerName != null
                    ? new Key(
                        model.TriggerName,
                        model.TriggerGroup ?? BlazingQuartz.Constants.DEFAULT_GROUP
                    )
                    : null,
        };
        DialogSvc.ShowAsync<HistoryDialog>("Execution History", parameters, options);
    }

    private void OnLogs(ScheduleModel model)
    {
        if (model.JobName == null)
        {
            return;
        }

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large,
        };

        var parameters = new DialogParameters
        {
            ["JobKey"] = new Key(model.JobName, model.JobGroup),
            ["TriggerKey"] =
                model.TriggerName != null
                    ? new Key(
                        model.TriggerName,
                        model.TriggerGroup ?? BlazingQuartz.Constants.DEFAULT_GROUP
                    )
                    : null,
        };
        DialogSvc.ShowAsync<LogsDialog>("Logs", parameters, options);
    }

    private async Task OnTriggerNow(ScheduleModel model)
    {
        bool? confirmed = await DialogSvc.ShowMessageBox(
            title: "Confirm",
            markupMessage: (MarkupString)"Do you want to trigger this job now?",
            yesText: "Trigger",
            cancelText: "Cancel"
        );

        if (confirmed != true)
            return;

        var result = await SchedulerWorkflow.TriggerJobNowAsync(model);
        if (!result.IsSuccess)
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
    }
}
