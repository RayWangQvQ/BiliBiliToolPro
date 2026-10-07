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
        var message = new StringBuilder("本批任务执行完毕，以下任务执行失败：\n\n");
        foreach (var item in summary.Items)
            message.AppendLine(
                $"- {item.TaskName}：{item.FailureCount} 次失败（账号 {string.Join("、", item.MaskedAccounts)}）"
            );
        message.AppendLine(
            "\n请打开面板「今日任务」或执行记录查看详情，按需补做。\n\n本消息为本批任务的统一汇总提醒。"
        );
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["title"] = "BiliBili 任务失败汇总",
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
