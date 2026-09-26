using System;
using BlazingQuartz.Core.Events;
using Quartz;

namespace BlazingQuartz.Core.Services
{
    public class SchedulerListenerService
        : ISchedulerListenerService,
            IJobListener,
            ITriggerListener,
            ISchedulerListener
    {
        public event EventHandler<EventArgs<IJobDetail>>? OnJobAdded;
        public event EventHandler<EventArgs<JobKey>>? OnJobDeleted;
        public event EventHandler<EventArgs<IJobExecutionContext>>? OnJobExecutionVetoed;
        public event EventHandler<EventArgs<JobKey>>? OnJobInterrupted;
        public event EventHandler<EventArgs<JobKey>>? OnJobPaused;
        public event EventHandler<EventArgs<JobKey>>? OnJobResumed;
        public event EventHandler<EventArgs<ITrigger>>? OnJobScheduled;
        public event EventHandler<EventArgs<string>>? OnJobsPaused;
        public event EventHandler<EventArgs<string>>? OnJobsResumed;
        public event EventHandler<EventArgs<IJobExecutionContext>>? OnJobToBeExecuted;
        public event EventHandler<EventArgs<TriggerKey>>? OnJobUnscheduled;
        public event EventHandler<JobWasExecutedEventArgs>? OnJobWasExecuted;
        public event EventHandler<SchedulerErrorEventArgs>? OnSchedulerError;
        public event EventHandler<CancellationToken>? OnSchedulerInStandbyMode;
        public event EventHandler<CancellationToken>? OnSchedulerShutdown;
        public event EventHandler<CancellationToken>? OnSchedulerShuttingdown;
        public event EventHandler<CancellationToken>? OnSchedulerStarted;
        public event EventHandler<CancellationToken>? OnSchedulerStarting;
        public event EventHandler<CancellationToken>? OnSchedulingDataCleared;
        public event EventHandler<EventArgs<ITrigger>>? OnTriggerFinalized;
        public event EventHandler<EventArgs<ITrigger>>? OnTriggerMisfired;
        public event EventHandler<EventArgs<TriggerKey>>? OnTriggerPaused;
        public event EventHandler<EventArgs<TriggerKey>>? OnTriggerResumed;
        public event EventHandler<EventArgs<TriggerKey>>? OnTriggerInError;
        public event EventHandler<EventArgs<JobKey>>? OnTriggersInError;
        public event EventHandler<EventArgs<string?>>? OnTriggerGroupPaused;
        public event EventHandler<EventArgs<string?>>? OnTriggerGroupResumed;
        public event EventHandler<TriggerEventArgs>? OnTriggerComplete;
        public event EventHandler<TriggerEventArgs>? OnTriggerFired;

        public string Name => "BlazingQuartzNetUI";

        public ValueTask JobAdded(
            IScheduler scheduler,
            IJobDetail jobDetail,
            CancellationToken cancellationToken = default
        )
        {
            OnJobAdded?.Invoke(this, new EventArgs<IJobDetail>(jobDetail, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobDeleted(
            IScheduler scheduler,
            JobKey jobKey,
            CancellationToken cancellationToken = default
        )
        {
            OnJobDeleted?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobExecutionVetoed(
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            OnJobExecutionVetoed?.Invoke(
                this,
                new EventArgs<IJobExecutionContext>(context, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask JobInterrupted(
            IScheduler scheduler,
            JobKey jobKey,
            CancellationToken cancellationToken = default
        )
        {
            OnJobInterrupted?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobPaused(
            IScheduler scheduler,
            JobKey jobKey,
            CancellationToken cancellationToken = default
        )
        {
            OnJobPaused?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobResumed(
            IScheduler scheduler,
            JobKey jobKey,
            CancellationToken cancellationToken = default
        )
        {
            OnJobResumed?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobScheduled(
            IScheduler scheduler,
            ITrigger trigger,
            CancellationToken cancellationToken = default
        )
        {
            OnJobScheduled?.Invoke(this, new EventArgs<ITrigger>(trigger, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobsPaused(
            IScheduler scheduler,
            string jobGroup,
            CancellationToken cancellationToken = default
        )
        {
            OnJobsPaused?.Invoke(this, new EventArgs<string>(jobGroup, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobsResumed(
            IScheduler scheduler,
            string jobGroup,
            CancellationToken cancellationToken = default
        )
        {
            OnJobsResumed?.Invoke(this, new EventArgs<string>(jobGroup, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask JobToBeExecuted(
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            OnJobToBeExecuted?.Invoke(
                this,
                new EventArgs<IJobExecutionContext>(context, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask JobUnscheduled(
            IScheduler scheduler,
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnJobUnscheduled?.Invoke(
                this,
                new EventArgs<TriggerKey>(triggerKey, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask JobWasExecuted(
            IJobExecutionContext context,
            JobExecutionException? jobException,
            CancellationToken cancellationToken = default
        )
        {
            OnJobWasExecuted?.Invoke(
                this,
                new JobWasExecutedEventArgs(context, jobException, cancellationToken)
                {
                    JobException = jobException,
                }
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulerError(
            IScheduler scheduler,
            SchedulerErrorContext errorContext,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerError?.Invoke(
                this,
                new SchedulerErrorEventArgs
                {
                    ErrorMessage = errorContext.Message,
                    Exception = errorContext.Exception,
                    CancelToken = cancellationToken,
                }
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulerInStandbyMode(
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerInStandbyMode?.Invoke(this, cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulerShutdown(
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerShutdown?.Invoke(this, cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulerShuttingDown(
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerShuttingdown?.Invoke(this, cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulerStarted(
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerStarted?.Invoke(this, cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulerStarting(
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerStarting?.Invoke(this, cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask SchedulingDataCleared(
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulingDataCleared?.Invoke(this, cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerComplete(
            ITrigger trigger,
            IJobExecutionContext context,
            SchedulerInstruction triggerInstructionCode,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerComplete?.Invoke(
                this,
                new TriggerEventArgs(trigger, context, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerFinalized(
            IScheduler scheduler,
            ITrigger trigger,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerFinalized?.Invoke(this, new EventArgs<ITrigger>(trigger, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerFired(
            ITrigger trigger,
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerFired?.Invoke(this, new TriggerEventArgs(trigger, context, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerInError(
            IScheduler scheduler,
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerInError?.Invoke(
                this,
                new EventArgs<TriggerKey>(triggerKey, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerMisfired(
            ITrigger trigger,
            IScheduler scheduler,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerMisfired?.Invoke(this, new EventArgs<ITrigger>(trigger, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerPaused(
            IScheduler scheduler,
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerPaused?.Invoke(this, new EventArgs<TriggerKey>(triggerKey, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggerResumed(
            IScheduler scheduler,
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerResumed?.Invoke(
                this,
                new EventArgs<TriggerKey>(triggerKey, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggersInError(
            IScheduler scheduler,
            JobKey jobKey,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggersInError?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggersPaused(
            IScheduler scheduler,
            string? triggerGroup,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerGroupPaused?.Invoke(
                this,
                new EventArgs<string?>(triggerGroup, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask TriggersResumed(
            IScheduler scheduler,
            string? triggerGroup,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerGroupResumed?.Invoke(
                this,
                new EventArgs<string?>(triggerGroup, cancellationToken)
            );
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> VetoJobExecution(
            ITrigger trigger,
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            return new ValueTask<bool>(false);
        }
    }
}
