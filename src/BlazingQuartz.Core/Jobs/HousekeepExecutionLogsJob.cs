using System;
using BlazingQuartz.Core.History;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;

namespace BlazingQuartz.Core.Jobs
{
    public class HousekeepExecutionLogsJob : IJob
    {
        private readonly IExecutionLogStore _logStore;
        private readonly BlazingQuartzCoreOptions _options;

        public HousekeepExecutionLogsJob(
            IExecutionLogStore logStore,
            IOptions<BlazingQuartzCoreOptions> options
        )
        {
            _logStore = logStore;
            _options = options.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var count = await _logStore.DeleteLogsByDays(_options.ExecutionLogsDaysToKeep);
                context.Result = $"Deleted {count} record(s)";
                context.SetIsSuccess(true);
            }
            catch (Exception ex)
            {
                throw new JobExecutionException("Failed to delete execution logs", ex);
            }
        }
    }
}
