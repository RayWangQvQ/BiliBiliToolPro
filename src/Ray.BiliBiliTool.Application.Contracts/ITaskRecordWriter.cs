using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Application.Contracts;

/// <summary>
/// 任务执行记录的写入接口。由 Web 层用 EF 实现，Application 层只依赖此抽象。
/// </summary>
public interface ITaskRecordWriter
{
    Task WriteAsync(
        long userId,
        string taskKey,
        string? taskItemKey,
        TaskRecordStatus status,
        string? message,
        TaskRecordTrigger trigger,
        CancellationToken cancellationToken = default
    );
}
