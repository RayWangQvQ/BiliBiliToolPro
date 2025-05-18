using System;
using Ray.BiliBiliTool.Domain;

namespace BlazingQuartz.Core.Models
{
    public class ExecutionLogFilter : ICloneable
    {
        public string? JobName { get; set; }
        public string? JobGroup { get; set; }
        public string? TriggerName { get; set; }
        public string? TriggerGroup { get; set; }
        public HashSet<LogType>? LogTypes { get; set; }
        public string? MessageContains { get; set; }

        /// <summary>
        /// If only StartUtc specified, means anything after this date
        /// </summary>
        public DateTimeOffset? DateAddedStartUtc { get; set; }

        /// <summary>
        /// DateAddedUtc before this date (exclusive).
        /// <para>If only EndUtc specified, means anything before this date</para>
        /// </summary>
        public DateTimeOffset? DateAddedEndUtc { get; set; }
        public bool IsAscending { get; set; }
        public bool ErrorOnly { get; set; }
        public bool IncludeSystemJobs { get; set; } = false;

        public object Clone()
        {
            var newObj = (ExecutionLogFilter)this.MemberwiseClone();
            if (this.LogTypes != null)
            {
                newObj.LogTypes = new HashSet<LogType>(this.LogTypes);
            }

            return newObj;
        }
    }
}
