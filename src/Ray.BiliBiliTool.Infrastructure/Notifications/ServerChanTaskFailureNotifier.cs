using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Notifications;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public sealed class ServerChanTaskFailureNotifier(
    IHttpClientFactory clientFactory,
    IConfiguration configuration
) : ITaskFailureNotifier
{
    public async Task<bool> SendAsync(
        TaskFailureSummary summary,
        CancellationToken cancellationToken
    )
    {
        if (
            !configuration.GetValue("TaskFailureNotification:Enabled", true)
            || summary.Items.Count == 0
        )
            return false;
        var endpoint = ServerChanCookieExpiryNotifier.CreateEndpoint(
            ServerChanCookieExpiryNotifier.GetSendKey(configuration)
        );
        if (endpoint is null)
            return false;
        var message = new StringBuilder(
            $"{summary.Day?.ToString("yyyy-MM-dd") ?? summary.CompletedAtUtc.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd")} 自动任务汇总\n\n"
        );
        message.AppendLine(
            summary.CutoffReached ? "已到设定的最终汇总时间。\n" : "当天自动任务已全部结束。\n"
        );
        foreach (var item in summary.Items)
        {
            message.AppendLine($"- {item.TaskName}：{(item.Completed ? "已结束" : "未完成")}");
            if (item.FailureCount > 0)
                message.AppendLine(
                    $"  今日失败 {item.FailureCount} 次（账号 {string.Join("、", item.MaskedAccounts)}）"
                );
            if (item.PendingAccounts is { Count: > 0 })
                message.AppendLine($"  未完成账号：{string.Join("、", item.PendingAccounts)}");
        }
        message.AppendLine(
            "\n请打开面板「今日任务」或执行记录查看详情。\n\n本消息为当天自动任务的统一汇总提醒。"
        );
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["title"] = "BiliBili 每日任务汇总",
                ["desp"] = message.ToString(),
            }
        );
        using var response = await clientFactory
            .CreateClient(ServerChanCookieExpiryNotifier.ClientName)
            .PostAsync(endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        return body.RootElement.TryGetProperty("code", out var code)
            && code.TryGetInt32(out var value)
            && value == 0;
    }
}
