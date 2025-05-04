using System;
using Quartz;

namespace BlazingQuartz.Core.Events
{
    public class JobWasExecutedEventArgs : EventArgs
    {
        public IJobExecutionContext JobExecutionContext { get; init; }
        public JobExecutionException? JobException { get; init; }
        public CancellationToken CancelToken { get; set; }

        public JobWasExecutedEventArgs(
            IJobExecutionContext context,
            JobExecutionException? exception,
            CancellationToken cancelToken = default(CancellationToken)
        )
        {
            JobExecutionContext = context;
            JobException = exception;
            CancelToken = cancelToken;
        }
    }
}
