using System.Text;
using Ray.BiliBiliTool.Application.Contracts.Notifications;

namespace Ray.BiliBiliTool.Application.Notifications;

/// <summary>
/// 通知文本格式化工具，供各推送通道复用
/// </summary>
public static class NotificationTextFormatter
{
    public static string FormatMessage(NotificationMessage message)
        => $"{message.Title}: {message.Body}";

    public static string FormatSummary(string title, SummaryLine[] lines)
    {
        StringBuilder builder = new();
        builder.AppendLine(title);

        foreach (SummaryLine line in lines)
        {
            string icon = line.StatusIcon ?? "•";
            builder.AppendLine($"{icon} {line.Label}: {line.Value}");
        }

        return builder.ToString().TrimEnd();
    }
}
