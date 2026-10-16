using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Web.Jobs;

public class DailyJob(
    ILogger<DailyJob> logger,
    IDailyTaskAppService appService,
    INotificationService notificationService,
    IOptionsMonitor<NotificationOptions> notificationOptions
) : BaseJob<DailyJob>(logger)
{
    public static readonly JobKey Key = new(nameof(DailyJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context) =>
        await appService.DoTaskAsync();

    protected override async Task SendCompletedNotificationAsync(
        IJobExecutionContext context,
        TimeSpan duration
    )
    {
        if (!notificationOptions.CurrentValue.SendSummaryOnSuccess)
            return;

        DateTimeOffset startedAt = DateTimeOffset.Now - duration;
        var lines = new[]
        {
            new SummaryLine("任务", "每日任务", "📌"),
            new SummaryLine("执行时间", startedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
            new SummaryLine("耗时", $"{(int)duration.TotalSeconds} 秒"),
            new SummaryLine("状态", "✅ 完成", "✅"),
        };

        await notificationService.SendSummaryAsync(
            "每日任务执行完成",
            lines,
            context.FireInstanceId
        );
    }

    protected override async Task SendErrorNotificationAsync(
        IJobExecutionContext context,
        Exception exception
    )
    {
        if (!notificationOptions.CurrentValue.SendOnError)
            return;

        await notificationService.SendAsync(
            new NotificationMessage("每日任务执行失败", exception.Message, NotificationLevel.Error),
            context.FireInstanceId
        );
    }
}
