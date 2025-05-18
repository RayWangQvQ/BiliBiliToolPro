using System;

namespace BlazingQuartz.Core.Models
{
    public class JobExecutionStatusSummaryModel
    {
        public DateTime StartDateTimeUtc { get; set; }
        public List<KeyValuePair<JobExecutionStatus, int>> Data { get; set; } = new();
    }
}
