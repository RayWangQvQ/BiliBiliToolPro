using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ray.BiliBiliTool.Domain;

/// <summary>
/// 账号任务执行记录：某个账号在某天执行了某项任务（或子项），结果如何。
/// 用于回答「今天这个账号的这个任务到底跑没跑过」。
/// </summary>
[Table("bili_task_records")]
public class TaskRecord
{
    [Key]
    public long Id { get; set; }

    /// <summary>B站 UID</summary>
    public long UserId { get; set; }

    /// <summary>任务键（AppService 类型名，如 DailyTaskAppService）</summary>
    [MaxLength(64)]
    public string TaskKey { get; set; } = "";

    /// <summary>子项键（Login / Watch / Share / DonateCoin / VipPrivilege）；null 表示任务级记录</summary>
    [MaxLength(64)]
    public string? TaskItemKey { get; set; }

    /// <summary>本地日期（Asia/Shanghai），格式 yyyy-MM-dd</summary>
    [MaxLength(10)]
    public string RecordDate { get; set; } = "";

    public TaskRecordStatus Status { get; set; }

    /// <summary>失败原因摘要</summary>
    [MaxLength(512)]
    public string? Message { get; set; }

    public TaskRecordTrigger Trigger { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public enum TaskRecordStatus
{
    Success,
    Failed,
}

public enum TaskRecordTrigger
{
    /// <summary>定时任务自己执行的</summary>
    Scheduled,

    /// <summary>自动补做</summary>
    Auto,

    /// <summary>页面手动补做</summary>
    Manual,
}
