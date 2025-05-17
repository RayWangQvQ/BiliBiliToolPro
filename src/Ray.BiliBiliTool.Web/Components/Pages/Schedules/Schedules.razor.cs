using System.Collections.ObjectModel;
using BlazingQuartz;
using BlazingQuartz.Core.Events;
using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Quartz;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Client;
using Ray.BiliBiliTool.Web.Components.Comps;
using Ray.BiliBiliTool.Web.Extensions;

namespace Ray.BiliBiliTool.Web.Components.Pages.Schedules;

public partial class Schedules : ComponentBase, IDisposable
{
    private ScheduleJobFilter _filter = new();
    private readonly Func<ScheduleModel, object> _groupDefinition = x => x.JobGroup;
    private MudDataGrid<ScheduleModel> _scheduleDataGrid = new();

    [Inject]
    private ILogger<Schedules> Logger { get; set; } = null!;

    [Inject]
    private ISchedulerService SchedulerSvc { get; set; } = null!;

    [Inject]
    private ISchedulerListenerService SchedulerListenerSvc { get; set; } = null!;

    [Inject]
    private IExecutionLogService ExecutionLogSvc { get; set; } = null!;

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

    internal bool IsEditActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule
        || model.JobStatus == JobStatus.Error
        || model.JobStatus == JobStatus.Running
        || model.JobGroup == BlazingQuartz.Constants.SYSTEM_GROUP;

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

    internal bool IsAddTriggerActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule
        || model.JobStatus == JobStatus.Error
        || model.JobGroup == BlazingQuartz.Constants.SYSTEM_GROUP;

    internal bool IsCopyActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule
        || model.JobStatus == JobStatus.Error
        || model.JobGroup == BlazingQuartz.Constants.SYSTEM_GROUP;

    internal bool IsHistoryActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.NoSchedule;

    internal bool IsDeleteActionDisabled(ScheduleModel model) =>
        model.JobStatus == JobStatus.Running;

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

        IAsyncEnumerable<ScheduleModel> jobs = SchedulerSvc.GetAllJobsAsync(_filter);
        await foreach (ScheduleModel job in jobs)
        {
            ScheduledJobs.Add(job);
        }

        if (ScheduledJobs.Any())
            await _scheduleDataGrid.ExpandAllGroupsAsync();

        await UpdateScheduleModelsLastExecution();
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
                    JobDetailModel? jobDetail = await SchedulerSvc.GetJobDetail(
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
            ScheduleModel model = await SchedulerSvc.GetScheduleModelAsync(e.Args);
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

    private async Task UpdateScheduleModelsLastExecution()
    {
        var latestResult = new PageMetadata(0, 1);
        var scheduleJobType = new HashSet<LogType> { LogType.ScheduleJob };

        foreach (ScheduleModel schModel in ScheduledJobs)
        {
            if (string.IsNullOrEmpty(schModel.JobName))
                continue;

            PagedList<ExecutionLog> latestLogList = await ExecutionLogSvc.GetLatestExecutionLog(
                schModel.JobName,
                schModel.JobGroup,
                schModel.TriggerName,
                schModel.TriggerGroup,
                latestResult,
                logTypes: scheduleJobType
            );

            if (latestLogList != null && latestLogList.Any())
            {
                ExecutionLog latestLog = latestLogList.First();
                if (!schModel.PreviousTriggerTime.HasValue)
                {
                    schModel.PreviousTriggerTime = latestLog.FireTimeUtc;
                }

                if (latestLog.IsSuccess.HasValue && !latestLog.IsSuccess.Value)
                {
                    schModel.ExceptionMessage = latestLog.GetShortResultMessage();
                }
                else if (latestLog.IsException ?? false)
                {
                    schModel.ExceptionMessage = latestLog.GetShortExceptionMessage();
                }
            }
        }
    }

    private async Task OnNewSchedule()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
        };
        IDialogReference dlg = DialogSvc.Show<ScheduleDialog>("Create Schedule Job", options);
        DialogResult? result = await dlg.Result;

        if (result == null || result.Canceled)
            return;

        // create schedule
        (JobDetailModel jobDetail, TriggerDetailModel triggerDetail) = ((
            JobDetailModel,
            TriggerDetailModel
        ))
            result.Data;

        try
        {
            await SchedulerSvc.CreateSchedule(jobDetail, triggerDetail);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create new schedule. {ex.Message}", Severity.Error);
            Logger.LogError(ex, "Failed to create new schedule.");
            // TODO show schedule dialog again?
        }
    }

    private async Task OnEditScheduleJob(ScheduleModel model)
    {
        if (model.JobName == null)
        {
            Snackbar.Add("Cannot edit schedule. Check if job still exists.", Severity.Error);
            return;
        }

        JobDetailModel? currentJobDetail = await SchedulerSvc.GetJobDetail(
            model.JobName,
            model.JobGroup
        );

        if (currentJobDetail == null)
        {
            Snackbar.Add("Cannot edit schedule. Check if job still exists.", Severity.Error);
            return;
        }

        var origJobKey = new Key(currentJobDetail.Name, currentJobDetail.Group);

        TriggerDetailModel? currentTriggerModel = null;
        Key? origTriggerKey = null;
        if (model.TriggerName != null)
        {
            currentTriggerModel = await SchedulerSvc.GetTriggerDetail(
                model.TriggerName,
                model?.TriggerGroup ?? BlazingQuartz.Constants.DEFAULT_GROUP
            );

            if (currentTriggerModel != null)
            {
                origTriggerKey = new Key(currentTriggerModel.Name, currentTriggerModel.Group);

                ResetStartEndDateTimeIfEarlier(ref currentTriggerModel);
            }
        }

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
        };
        var parameters = new DialogParameters
        {
            ["JobDetail"] = currentJobDetail,
            ["TriggerDetail"] = currentTriggerModel ?? new TriggerDetailModel(),
        };
        IDialogReference dlg = DialogSvc.Show<ScheduleDialog>(
            "Edit Schedule Job",
            parameters,
            options
        );
        DialogResult? result = await dlg.Result;

        if (result == null || result.Canceled)
            return;

        // update schedule
        (JobDetailModel jobDetail, TriggerDetailModel triggerDetail) = ((
            JobDetailModel,
            TriggerDetailModel
        ))
            result.Data;
        try
        {
            await SchedulerSvc.UpdateSchedule(origJobKey, origTriggerKey, jobDetail, triggerDetail);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to update schedule. {ex.Message}", Severity.Error);
            Logger.LogError(ex, "Failed to update schedule.");
            // TODO display the dialog again?
        }
    }

    private async Task OnResumeScheduleJob(ScheduleModel model)
    {
        if (model.TriggerName == null)
        {
            Snackbar.Add("Cannot resume schedule. Trigger name is null.", Severity.Error);
            return;
        }

        await SchedulerSvc.ResumeTrigger(model.TriggerName, model.TriggerGroup);
    }

    private async Task OnPauseScheduleJob(ScheduleModel model)
    {
        if (model.TriggerName == null)
        {
            Snackbar.Add("Cannot pause schedule. Trigger name is null.", Severity.Error);
            return;
        }

        await SchedulerSvc.PauseTrigger(model.TriggerName, model.TriggerGroup);
    }

    private async Task OnDeleteScheduleJob(ScheduleModel model)
    {
        if (model.JobStatus == JobStatus.NoSchedule)
        {
            ScheduledJobs.Remove(model);
        }
        else
        {
            // confirm delete
            bool? yes = await DialogSvc.ShowMessageBox(
                "Confirm Delete",
                "Do you want to delete this schedule?",
                "Yes",
                cancelText: "No"
            );
            if (yes == null || !yes.Value)
            {
                return;
            }

            bool success = await SchedulerSvc.DeleteSchedule(model);

            if (!success)
            {
                Snackbar.Add($"Failed to delete schedule '{model.JobName}'", Severity.Error);
            }
            else
            {
                Snackbar.Add("Deleted schedule", Severity.Info);
            }
        }
    }

    private async Task OnDuplicateScheduleJob(ScheduleModel model)
    {
        if (model.JobName == null)
        {
            Snackbar.Add("Cannot clone schedule. Check if job still exists.", Severity.Error);
            return;
        }

        JobDetailModel? currentJobDetail = await SchedulerSvc.GetJobDetail(
            model.JobName,
            model.JobGroup
        );

        if (currentJobDetail == null)
        {
            Snackbar.Add("Cannot clone schedule. Check if job still exists.", Severity.Error);
            return;
        }

        TriggerDetailModel? currentTriggerModel = null;
        if (model.TriggerName != null)
        {
            currentTriggerModel = await SchedulerSvc.GetTriggerDetail(
                model.TriggerName,
                model?.TriggerGroup ?? BlazingQuartz.Constants.DEFAULT_GROUP
            );
            if (currentTriggerModel != null)
            {
                currentTriggerModel.Name = string.Empty;
                ResetStartEndDateTimeIfEarlier(ref currentTriggerModel);
            }
        }

        currentJobDetail.Name = string.Empty;

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
        };
        var parameters = new DialogParameters
        {
            ["JobDetail"] = currentJobDetail,
            ["TriggerDetail"] = currentTriggerModel ?? new TriggerDetailModel(),
        };
        IDialogReference dlg = DialogSvc.Show<ScheduleDialog>(
            "Create Schedule Job",
            parameters,
            options
        );
        DialogResult? result = await dlg.Result;

        if (result == null || result.Canceled)
            return;

        // create schedule
        (JobDetailModel jobDetail, TriggerDetailModel triggerDetail) = ((
            JobDetailModel,
            TriggerDetailModel
        ))
            result.Data;
        await SchedulerSvc.CreateSchedule(jobDetail, triggerDetail);
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
        if (model.JobName == null)
        {
            Snackbar.Add("Cannot add trigger. Check if job still exists.", Severity.Error);
            return;
        }

        bool? result = await DialogSvc.ShowMessageBox(
            title: "Confirm",
            markupMessage: (MarkupString)"Do you want to trigger this job now?",
            yesText: "Trigger",
            cancelText: "Cancel"
        );

        if (result != true)
        {
            return;
        }

        await SchedulerSvc.TriggerJob(model.JobName, model.JobGroup);
    }

    private async Task OnAddTrigger(ScheduleModel model)
    {
        if (model.JobName == null)
        {
            Snackbar.Add("Cannot add trigger. Check if job still exists.", Severity.Error);
            return;
        }

        JobDetailModel? currentJobDetail = await SchedulerSvc.GetJobDetail(
            model.JobName,
            model.JobGroup
        );

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
        };
        var parameters = new DialogParameters
        {
            ["JobDetail"] = currentJobDetail,
            ["IsReadOnlyJobDetail"] = true,
            ["SelectedTab"] = ScheduleDialogTab.Trigger,
        };
        IDialogReference dlg = DialogSvc.Show<ScheduleDialog>(
            "Add New Trigger",
            parameters,
            options
        );
        DialogResult? result = await dlg.Result;

        if (result == null || result.Canceled)
            return;

        // create schedule
        (JobDetailModel jobDetail, TriggerDetailModel triggerDetail) = ((
            JobDetailModel,
            TriggerDetailModel
        ))
            result.Data;
        await SchedulerSvc.CreateSchedule(jobDetail, triggerDetail);
    }

    private async Task OnDeleteSelectedScheduleJobs()
    {
        if (_scheduleDataGrid is null)
            return;

        HashSet<ScheduleModel>? selectedItems = _scheduleDataGrid.SelectedItems;

        if (selectedItems == null || selectedItems.Count == 0)
            return;

        // confirm delete
        bool? yes = await DialogSvc.ShowMessageBox(
            "Confirm Delete",
            $"Do you want to delete selected {selectedItems.Count} schedules?",
            "Yes",
            cancelText: "No"
        );
        if (yes == null || !yes.Value)
        {
            return;
        }

        int skipCount = 0;

        IEnumerable<Task<bool>> deleteTasks = selectedItems.Select(model =>
        {
            if (model.JobStatus == JobStatus.Running)
            {
                skipCount++;
                return Task.FromResult(true);
            }

            ScheduledJobs.Remove(model);
            return SchedulerSvc.DeleteSchedule(model);
        });
        bool[] results = await Task.WhenAll(deleteTasks);

        if (results == null)
        {
            await RefreshJobs();
            Snackbar.Add("Failed to delete schedules", Severity.Error);
        }
        else
        {
            int deletedCount = results.Where(t => t).Count();
            int notDeletedCount = results.Count() - deletedCount - skipCount;

            if (skipCount > 0)
            {
                Snackbar.Add(
                    $"Deleted {deletedCount} schedule(s). Skip {skipCount} executing schedule(s)",
                    Severity.Info
                );
            }
            else
            {
                Snackbar.Add($"Deleted {deletedCount} schedule(s)", Severity.Info);
            }

            if (notDeletedCount > 0)
            {
                await RefreshJobs();
                Snackbar.Add($"Failed to deleted {notDeletedCount} schedule(s)", Severity.Warning);
            }
        }
    }

    private void ResetStartEndDateTimeIfEarlier(ref TriggerDetailModel triggerModel)
    {
        DateTimeOffset? startDtime = triggerModel.StartDateTimeUtc;
        if (startDtime.HasValue && startDtime <= DateTimeOffset.UtcNow)
        {
            // clear start date if already past
            triggerModel.StartTimeSpan = null;
            triggerModel.StartDate = null;
            triggerModel.StartTimezone = TimeZoneInfo.Utc;
        }

        DateTimeOffset? endTime = triggerModel.EndDateTimeUtc;
        if (endTime.HasValue && endTime <= DateTimeOffset.UtcNow)
        {
            // clear end date if already past
            triggerModel.EndDate = null;
            triggerModel.EndTimeSpan = null;
        }
    }
}
