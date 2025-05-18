using System;
using BlazingQuartz.Core.Events;
using Quartz;

namespace BlazingQuartz.Core.Services
{
    public interface ISchedulerListenerService
    {
        event EventHandler<EventArgs<IJobDetail>>? OnJobAdded;
        event EventHandler<EventArgs<JobKey>>? OnJobDeleted;

        /// <summary>
        /// From IJobListener
        /// </summary>
        event EventHandler<EventArgs<IJobExecutionContext>>? OnJobExecutionVetoed;
        event EventHandler<EventArgs<JobKey>>? OnJobInterrupted;
        event EventHandler<EventArgs<JobKey>>? OnJobPaused;
        event EventHandler<EventArgs<JobKey>>? OnJobResumed;
        event EventHandler<EventArgs<ITrigger>>? OnJobScheduled;
        event EventHandler<EventArgs<string>>? OnJobsPaused;
        event EventHandler<EventArgs<string>>? OnJobsResumed;

        /// <summary>
        /// From IJobListener
        /// </summary>
        event EventHandler<EventArgs<IJobExecutionContext>>? OnJobToBeExecuted;
        event EventHandler<EventArgs<TriggerKey>>? OnJobUnscheduled;

        /// <summary>
        /// From IJobListener
        /// </summary>
        event EventHandler<JobWasExecutedEventArgs>? OnJobWasExecuted;
        event EventHandler<SchedulerErrorEventArgs>? OnSchedulerError;
        event EventHandler<CancellationToken>? OnSchedulerInStandbyMode;
        event EventHandler<CancellationToken>? OnSchedulerShutdown;
        event EventHandler<CancellationToken>? OnSchedulerShuttingdown;
        event EventHandler<CancellationToken>? OnSchedulerStarted;
        event EventHandler<CancellationToken>? OnSchedulerStarting;
        event EventHandler<CancellationToken>? OnSchedulingDataCleared;
        event EventHandler<EventArgs<ITrigger>>? OnTriggerFinalized;

        /// <summary>
        /// From ITriggerListener
        /// </summary>
        event EventHandler<EventArgs<ITrigger>>? OnTriggerMisfired;
        event EventHandler<EventArgs<TriggerKey>>? OnTriggerPaused;
        event EventHandler<EventArgs<TriggerKey>>? OnTriggerResumed;
        event EventHandler<EventArgs<string?>>? OnTriggerGroupPaused;
        event EventHandler<EventArgs<string?>>? OnTriggerGroupResumed;

        /// <summary>
        /// From ITriggerListener
        /// </summary>
        event EventHandler<TriggerEventArgs>? OnTriggerComplete;

        /// <summary>
        /// From ITriggerListener
        /// </summary>
        event EventHandler<TriggerEventArgs>? OnTriggerFired;
    }
}
