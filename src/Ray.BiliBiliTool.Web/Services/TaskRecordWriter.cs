using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 任务执行记录的 EF 实现。写入失败只记日志，绝不影响任务本身。
/// </summary>
public class TaskRecordWriter(
    IDbContextFactory<BiliDbContext> dbContextFactory,
    ILogger<TaskRecordWriter> logger
) : ITaskRecordWriter
{
    /// <summary>记录统一使用本地日期（容器时区为 Asia/Shanghai）</summary>
    public static string TodayDateKey() => DateTimeOffset.Now.ToString("yyyy-MM-dd");

    public async Task WriteAsync(
        long userId,
        string taskKey,
        string? taskItemKey,
        TaskRecordStatus status,
        string? message,
        TaskRecordTrigger trigger,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            db.TaskRecords.Add(
                new TaskRecord
                {
                    UserId = userId,
                    TaskKey = taskKey,
                    TaskItemKey = taskItemKey,
                    RecordDate = TodayDateKey(),
                    Status = status,
                    Message = Truncate(message, 512),
                    Trigger = trigger,
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // 记录失败不能影响任务本身
            logger.LogWarning(
                ex,
                "写入任务执行记录失败：{taskKey}/{itemKey}",
                taskKey,
                taskItemKey
            );
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
