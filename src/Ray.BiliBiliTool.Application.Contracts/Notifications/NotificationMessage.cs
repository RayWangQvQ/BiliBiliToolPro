namespace Ray.BiliBiliTool.Application.Contracts.Notifications;

/// <summary>
/// 通知消息载体，用于 SendAsync 的事件级通知
/// </summary>
/// <param name="Title">通知标题</param>
/// <param name="Body">通知正文</param>
/// <param name="Level">通知级别</param>
/// <param name="GroupKey">可选的批次分组键（方法参数优先于此字段）</param>
public record NotificationMessage(
    string Title,
    string Body,
    NotificationLevel Level,
    string? GroupKey = null
);
