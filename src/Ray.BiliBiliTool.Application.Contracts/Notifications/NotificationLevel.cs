namespace Ray.BiliBiliTool.Application.Contracts.Notifications;

/// <summary>
/// 通知级别（独立于 Serilog 的 LogEventLevel，端口与日志框架解耦）
/// </summary>
public enum NotificationLevel
{
    Info,
    Warning,
    Error,
}
