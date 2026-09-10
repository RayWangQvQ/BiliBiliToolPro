namespace Ray.BiliBiliTool.Application.Contracts.Notifications;

/// <summary>
/// 任务完成摘要中的单行，用于 SendSummaryAsync 的结构化摘要
/// </summary>
/// <param name="Label">摘要项名称（如 "投币"）</param>
/// <param name="Value">摘要项结果（如 "5 个"）</param>
/// <param name="StatusIcon">可选的状态图标（如 "✅"、"❌"）</param>
public record SummaryLine(string Label, string Value, string? StatusIcon = null);
