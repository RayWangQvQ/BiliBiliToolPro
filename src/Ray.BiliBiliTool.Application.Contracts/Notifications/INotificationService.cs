namespace Ray.BiliBiliTool.Application.Contracts.Notifications;

/// <summary>
/// 通知服务端口 — 应用层通过此接口发送通知，不关心底层是 Serilog 还是 Telegram HTTP
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送单条事件通知
    /// </summary>
    /// <param name="message">通知消息</param>
    /// <param name="groupKey">批次分组键（通常是 fireInstanceId）</param>
    Task SendAsync(NotificationMessage message, string groupKey);

    /// <summary>
    /// 发送任务完成摘要
    /// </summary>
    /// <param name="title">摘要标题（通常是任务名称，如 "Daily"）</param>
    /// <param name="lines">摘要行列表</param>
    /// <param name="groupKey">批次分组键（通常是 fireInstanceId）</param>
    Task SendSummaryAsync(string title, SummaryLine[] lines, string groupKey);
}
