using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Jobs;

/// <summary>
/// 自动补做：每隔 N 小时检查一次，把今天到点但没做（或做了失败但没到重试上限）的项补上。
/// 「哪些项允许自动补做」见 <see cref="TaskStatusEvaluator.CanAutoRedo"/>。
///
/// 注意：Web.Jobs 不允许直接依赖 Infrastructure / Infrastructure.EF（见 DependencyGuardrailTests），
/// 所以清理过期记录走 ITodayTaskService，不在本类里碰 DbContext。
/// </summary>
public class AutoRecoverJob(
    ILogger<AutoRecoverJob> logger,
    IOptionsMonitor<AutoRecoverOptions> options,
    ITodayTaskService todayTaskService
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

        await todayTaskService.CleanupExpiredRecordsAsync(
            config.RecordRetentionDays,
            context.CancellationToken
        );

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
}
