using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ray.BiliBiliTool.Domain;

[Table("bili_execution_logs")]
public class ExecutionLog
{
    [Key]
    public long LogId { get; set; }

    [MaxLength(256)]
    public string? RunInstanceId { get; set; }

    [Column(TypeName = "varchar(20)")]
    public LogType LogType { get; set; }

    [MaxLength(256)]
    public string? JobName { get; set; }

    [MaxLength(256)]
    public string? JobGroup { get; set; }

    [MaxLength(256)]
    public string? TriggerName { get; set; }

    [MaxLength(256)]
    public string? TriggerGroup { get; set; }

    /// <summary>
    /// Expected time the job should get triggered
    /// </summary>
    public DateTimeOffset? ScheduleFireTimeUtc { get; set; }

    /// <summary>
    /// Actual time the job got triggered
    /// </summary>
    public DateTimeOffset? FireTimeUtc { get; set; }

    public TimeSpan? JobRunTime { get; set; }
    public int? RetryCount { get; set; }

    [MaxLength(8000)]
    public string? Result { get; set; }

    [MaxLength(8000)]
    public string? ErrorMessage { get; set; }
    public bool? IsVetoed { get; set; }
    public bool? IsException { get; set; }

    /// <summary>
    /// Indicate whether the execution is successful or not.
    /// If <see cref="LogType"/> is <see cref="LogType.ScheduleJob"/>, it may have value:
    /// <para>true - If job does not return IsSuccess or when execution completed successfully</para>
    /// <para>false - Execution completed but return code is error or job throw an exception</para>
    /// <para>null - Job still running or terminated unexpectedly.</para>
    /// If <see cref="LogType"/> is not <see cref="LogType.ScheduleJob"/> value will be null.
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Return code of execution.
    /// <para>Ex.</para>
    /// <para>for HTTP call - 200, 404, 500 etc.</para>
    /// <para>for command line - 0 = success, -1 = failed</para>
    /// </summary>
    [MaxLength(28)]
    public string? ReturnCode { get; set; }

    public DateTimeOffset DateAddedUtc { get; set; }
    public ExecutionLogDetail? ExecutionLogDetail { get; set; }

    public ExecutionLog()
    {
        DateAddedUtc = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset? GetFinishTimeUtc() => FireTimeUtc?.Add(JobRunTime ?? TimeSpan.Zero);
}
