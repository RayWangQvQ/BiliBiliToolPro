using BlazingQuartz.Core.Events;
using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using Quartz;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;
using Xunit;
using SchedulesPage = Ray.BiliBiliTool.Web.Components.Pages.Schedules.Schedules;

namespace Ray.BiliBiliTool.Web.ComponentTests.Schedules;

public class SchedulesComponentTests : TestContext
{
    public SchedulesComponentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<SchedulesPage>>(
            NullLogger<SchedulesPage>.Instance
        );
        Services.AddSingleton<ISchedulerListenerService>(new FakeSchedulerListenerService());
    }

    [Fact]
    public void Schedules_OnInitialized_RendersWithoutException()
    {
        Services.AddSingleton<ISchedulerPageWorkflow>(new FakeSchedulerPageWorkflow());

        var cut = RenderComponent<SchedulesPage>();

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Schedules_WithWorkflowReturningEmptyJobs_RendersDataGrid()
    {
        Services.AddSingleton<ISchedulerPageWorkflow>(new FakeSchedulerPageWorkflow());

        var cut = RenderComponent<SchedulesPage>();

        // MudDataGrid is rendered (table element present)
        cut.Markup.Should().Contain("mud-table");
    }

    private sealed class FakeSchedulerPageWorkflow : ISchedulerPageWorkflow
    {
        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            yield break;
        }

        public IAsyncEnumerable<ScheduleModel> GetAllJobsAsync(ScheduleJobFilter? filter = null) =>
            EmptyAsync<ScheduleModel>();

        public Task<JobDetailModel?> GetJobDetailAsync(string? jobName, string? jobGroup) =>
            Task.FromResult<JobDetailModel?>(null);

        public Task<ScheduleModel> GetScheduleModelAsync(ITrigger trigger) =>
            Task.FromResult(new ScheduleModel());

        public Task EnrichWithLastExecutionAsync(IEnumerable<ScheduleModel> scheduleModels) =>
            Task.CompletedTask;

        public Task<SchedulerActionResult> ResumeJobAsync(ScheduleModel model) =>
            Task.FromResult(new SchedulerActionResult(true, null));

        public Task<SchedulerActionResult> PauseJobAsync(ScheduleModel model) =>
            Task.FromResult(new SchedulerActionResult(true, null));

        public Task<SchedulerActionResult> TriggerJobNowAsync(ScheduleModel model) =>
            Task.FromResult(new SchedulerActionResult(true, null));
    }

    private sealed class FakeSchedulerListenerService : ISchedulerListenerService
    {
        public event EventHandler<EventArgs<IJobDetail>>? OnJobAdded
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<JobKey>>? OnJobDeleted
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<IJobExecutionContext>>? OnJobExecutionVetoed
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<JobKey>>? OnJobInterrupted
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<JobKey>>? OnJobPaused
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<JobKey>>? OnJobResumed
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<ITrigger>>? OnJobScheduled
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<string>>? OnJobsPaused
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<string>>? OnJobsResumed
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<IJobExecutionContext>>? OnJobToBeExecuted
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<TriggerKey>>? OnJobUnscheduled
        {
            add { }
            remove { }
        }
        public event EventHandler<JobWasExecutedEventArgs>? OnJobWasExecuted
        {
            add { }
            remove { }
        }
        public event EventHandler<SchedulerErrorEventArgs>? OnSchedulerError
        {
            add { }
            remove { }
        }
        public event EventHandler<CancellationToken>? OnSchedulerInStandbyMode
        {
            add { }
            remove { }
        }
        public event EventHandler<CancellationToken>? OnSchedulerShutdown
        {
            add { }
            remove { }
        }
        public event EventHandler<CancellationToken>? OnSchedulerShuttingdown
        {
            add { }
            remove { }
        }
        public event EventHandler<CancellationToken>? OnSchedulerStarted
        {
            add { }
            remove { }
        }
        public event EventHandler<CancellationToken>? OnSchedulerStarting
        {
            add { }
            remove { }
        }
        public event EventHandler<CancellationToken>? OnSchedulingDataCleared
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<ITrigger>>? OnTriggerFinalized
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<ITrigger>>? OnTriggerMisfired
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<TriggerKey>>? OnTriggerPaused
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<TriggerKey>>? OnTriggerResumed
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<string?>>? OnTriggerGroupPaused
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs<string?>>? OnTriggerGroupResumed
        {
            add { }
            remove { }
        }
        public event EventHandler<BlazingQuartz.Core.Events.TriggerEventArgs>? OnTriggerComplete
        {
            add { }
            remove { }
        }
        public event EventHandler<BlazingQuartz.Core.Events.TriggerEventArgs>? OnTriggerFired
        {
            add { }
            remove { }
        }
    }
}
