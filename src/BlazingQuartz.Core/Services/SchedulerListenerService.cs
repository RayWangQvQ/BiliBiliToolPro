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
        public event EventHandler<EventArgs<string?>>? OnTriggerGroupPaused;
        public event EventHandler<EventArgs<string?>>? OnTriggerGroupResumed;
        public event EventHandler<TriggerEventArgs>? OnTriggerComplete;
        public event EventHandler<TriggerEventArgs>? OnTriggerFired;

        public string Name => "BlazingQuartzNetUI";

        public Task JobAdded(IJobDetail jobDetail, CancellationToken cancellationToken = default)
        {
            OnJobAdded?.Invoke(this, new EventArgs<IJobDetail>(jobDetail, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobDeleted(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            OnJobDeleted?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobExecutionVetoed(
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            OnJobExecutionVetoed?.Invoke(
                this,
                new EventArgs<IJobExecutionContext>(context, cancellationToken)
            );
            return Task.CompletedTask;
        }

        public Task JobInterrupted(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            OnJobInterrupted?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobPaused(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            OnJobPaused?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobResumed(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            OnJobResumed?.Invoke(this, new EventArgs<JobKey>(jobKey, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            OnJobScheduled?.Invoke(this, new EventArgs<ITrigger>(trigger, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobsPaused(string jobGroup, CancellationToken cancellationToken = default)
        {
            OnJobsPaused?.Invoke(this, new EventArgs<string>(jobGroup, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobsResumed(string jobGroup, CancellationToken cancellationToken = default)
        {
            OnJobsResumed?.Invoke(this, new EventArgs<string>(jobGroup, cancellationToken));
            return Task.CompletedTask;
        }

        public Task JobToBeExecuted(
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            OnJobToBeExecuted?.Invoke(
                this,
                new EventArgs<IJobExecutionContext>(context, cancellationToken)
            );
            return Task.CompletedTask;
        }

        public Task JobUnscheduled(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnJobUnscheduled?.Invoke(
                this,
                new EventArgs<TriggerKey>(triggerKey, cancellationToken)
            );
            return Task.CompletedTask;
        }

        public Task JobWasExecuted(
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
            return Task.CompletedTask;
        }

        public Task SchedulerError(
            string msg,
            SchedulerException cause,
            CancellationToken cancellationToken = default
        )
        {
            OnSchedulerError?.Invoke(
                this,
                new SchedulerErrorEventArgs
                {
                    ErrorMessage = msg,
                    Exception = cause,
                    CancelToken = cancellationToken,
                }
            );
            return Task.CompletedTask;
        }

        public Task SchedulerInStandbyMode(CancellationToken cancellationToken = default)
        {
            OnSchedulerInStandbyMode?.Invoke(this, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SchedulerShutdown(CancellationToken cancellationToken = default)
        {
            OnSchedulerShutdown?.Invoke(this, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SchedulerShuttingdown(CancellationToken cancellationToken = default)
        {
            OnSchedulerShuttingdown?.Invoke(this, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SchedulerStarted(CancellationToken cancellationToken = default)
        {
            OnSchedulerStarted?.Invoke(this, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SchedulerStarting(CancellationToken cancellationToken = default)
        {
            OnSchedulerStarting?.Invoke(this, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SchedulingDataCleared(CancellationToken cancellationToken = default)
        {
            OnSchedulingDataCleared?.Invoke(this, cancellationToken);
            return Task.CompletedTask;
        }

        public Task TriggerComplete(
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
            return Task.CompletedTask;
        }

        public Task TriggerFinalized(
            ITrigger trigger,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerFinalized?.Invoke(this, new EventArgs<ITrigger>(trigger, cancellationToken));
            return Task.CompletedTask;
        }

        public Task TriggerFired(
            ITrigger trigger,
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerFired?.Invoke(this, new TriggerEventArgs(trigger, context, cancellationToken));
            return Task.CompletedTask;
        }

        public Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            OnTriggerMisfired?.Invoke(this, new EventArgs<ITrigger>(trigger, cancellationToken));
            return Task.CompletedTask;
        }

        public Task TriggerPaused(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerPaused?.Invoke(this, new EventArgs<TriggerKey>(triggerKey, cancellationToken));
            return Task.CompletedTask;
        }

        public Task TriggerResumed(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerResumed?.Invoke(
                this,
                new EventArgs<TriggerKey>(triggerKey, cancellationToken)
            );
            return Task.CompletedTask;
        }

        public Task TriggersPaused(
            string? triggerGroup,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerGroupPaused?.Invoke(
                this,
                new EventArgs<string?>(triggerGroup, cancellationToken)
            );
            return Task.CompletedTask;
        }

        public Task TriggersResumed(
            string? triggerGroup,
            CancellationToken cancellationToken = default
        )
        {
            OnTriggerGroupResumed?.Invoke(
                this,
                new EventArgs<string?>(triggerGroup, cancellationToken)
            );
            return Task.CompletedTask;
        }

        public Task<bool> VetoJobExecution(
            ITrigger trigger,
            IJobExecutionContext context,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(false);
        }
    }
}
