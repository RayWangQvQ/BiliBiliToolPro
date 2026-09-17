using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

/// <summary>
/// 自动补做：每隔 N 小时检查一次，把今天到点但没做（或做了失败但没到重试上限）的项补上。
/// 判定规则见规格 §5。
/// </summary>
public class AutoRecoverJob(
    ILogger<AutoRecoverJob> logger,
    IOptionsMonitor<AutoRecoverOptions> options,
    ITodayTaskService todayTaskService,
    IDbContextFactory<BiliDbContext> dbContextFactory
) : BaseJob<AutoRecoverJob>(logger)
{
    public static readonly JobKey Key = new(nameof(AutoRecoverJob), Constants.BiliJobGroup);
    public static readonly TriggerKey TriggerKeyValue = new(
        $"{nameof(AutoRecoverJob)}.Interval.Trigger",
        Constants.BiliJobGroup
    );

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        var config = options.CurrentValue;

        await CleanupAsync(config.RecordRetentionDays, context.CancellationToken);

        if (!config.IsEnable)
        {
            logger.LogInformation("自动补做已配置为关闭，跳过");
            return;
        }

        var status = await todayTaskService.GetTodayStatusAsync(
            includeBili: true,
            forceRefresh: false,
            cancellationToken: context.CancellationToken
        );

        foreach (var account in status)
        {
            if (!account.IsCookieValid)
            {
                continue;
            }

            foreach (var group in account.Groups)
            {
                foreach (var item in group.Items)
                {
                    // 只补「漏做」与「执行过但失败且未达自动重试上限」的项。
                    // 已达上限(RetryExhausted)、等待执行、本日无需执行、已关闭、状态未知、已完成都不补；
                    // 分享恒不自动补做（见 TaskStatusEvaluator.CanAutoRedo）。
                    if (!item.CanAutoRedo)
                    {
                        continue;
                    }

                    var result = await todayTaskService.RedoAsync(
                        account.UserId,
                        group.TaskKey,
                        item.ItemKey,
                        TaskRecordTrigger.Auto,
                        context.CancellationToken
                    );
                    logger.LogInformation(
                        "自动补做 {user}/{task}/{item}：{result}",
                        account.UserId,
                        group.TaskKey,
                        item.ItemKey,
                        result.Message
                    );
                }
            }
        }
    }

    /// <summary>清理超过保留天数的执行记录</summary>
    private async Task CleanupAsync(int retentionDays, CancellationToken cancellationToken)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Clamp(retentionDays, 1, 90));
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await db
                .TaskRecords.Where(r => r.CreatedAtUtc < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "清理过期的任务执行记录失败");
        }
    }

    /// <summary>
    /// 按当前配置重建触发器（间隔小时数变了要重新调度）。
    /// </summary>
    public static async Task RescheduleAsync(ISchedulerFactory schedulerFactory)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler();
            if (!await scheduler.CheckExists(TriggerKeyValue))
            {
                return;
            }

            var newTrigger = TriggerBuilder
                .Create()
                .WithIdentity(TriggerKeyValue)
                .ForJob(Key)
                .StartAt(DateTimeOffset.UtcNow.AddMinutes(1))
                .WithSimpleSchedule(x => x.WithIntervalInHours(ReadIntervalHours()).RepeatForever())
                .Build();

            await scheduler.RescheduleJob(TriggerKeyValue, newTrigger);
        }
        catch
        {
            // 重新调度失败不影响已保存的配置；下次重启会按新值启动
        }
    }

    private static int ReadIntervalHours()
    {
        var config = Global.ServiceProviderRoot?.GetService<IConfiguration>();
        return Math.Clamp(config?.GetValue("AutoRecoverConfig:IntervalHours", 2) ?? 2, 1, 24);
    }
}
