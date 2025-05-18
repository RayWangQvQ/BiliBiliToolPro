using System;

namespace BlazingQuartz.Core
{
    public class BlazingQuartzCoreOptions
    {
        /// <summary>
        /// <para>Assembly files that contain IJob or IJobUI implementation use for creating schedule.</para>
        /// <para>Ex. Quartz.Jobs</para>
        /// <para>Or Jobs/Quartz.Jobs - if this file is under Job folder</para>
        /// </summary>
        public string[]? AllowedJobAssemblyFiles { get; set; }

        /// <summary>
        /// <para>Job types that are not allowed to be used for creating new Jobs using UI.</para>
        /// <para>Ex. Quartz.Job.NativeJob</para>
        /// </summary>
        public string[]? DisallowedJobTypes { get; set; }

        /// <summary>
        /// Storage to use to store execution history
        /// </summary>
        public DataStoreProvider DataStoreProvider { get; set; } = DataStoreProvider.Sqlite;
        public bool AutoMigrateDb { get; set; } = true;
        public string? HousekeepingCronSchedule { get; set; } = "0 0 1 * * ?";
        public int ExecutionLogsDaysToKeep { get; set; } = 21;
    }
}
