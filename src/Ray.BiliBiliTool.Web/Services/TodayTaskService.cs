using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.SQLite;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 「今日任务」页面的查询与补做编排。
///
/// 性能约定：账号列表、执行记录、到点判定全部来自本地（配置 / 数据库 / Quartz），
/// 不发任何网络请求；B 站状态只在 <c>includeBili: true</c> 时并发补查，并有缓存与超时。
///
/// 状态判定规则见 <see cref="TaskStatusEvaluator"/>，到点计算见 <see cref="TaskDueTimeCalculator"/>。
/// </summary>
public class TodayTaskService(
    CookieStrFactory<BiliCookie> cookieStrFactory,
    IConfiguration configuration,
    IDbContextFactory<BiliDbContext> dbContextFactory,
    ISchedulerFactory schedulerFactory,
    IAccountDomainService accountDomainService,
    ICoinDomainService coinDomainService,
    IBiliAccountPageWorkflow accountWorkflow,
    IBiliAccountProbe accountProbe,
    TaskRecoveryExecutor recoveryExecutor,
    ITaskRecordWriter recordWriter,
    ILogger<TodayTaskService> logger
) : ITodayTaskService
{
    /// <summary>自动补做次数上限</summary>
    public const int MaxAutoAttempts = 3;

    /// <summary>并发保护：自动补做与手动补做不能同时跑</summary>
    private static readonly SemaphoreSlim RedoLock = new(1, 1);

    private static readonly TimeSpan BiliQueryTimeout = TimeSpan.FromSeconds(20);

    public async Task<List<AccountTodayTasksDto>> GetTodayStatusAsync(
        bool includeBili,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.Now;
        var dateKey = now.ToString("yyyy-MM-dd");

        // 1) 账号列表：只读本地配置，不调 B 站
        var rawAccounts = await accountWorkflow.GetAllAccountsAsync();
        var accounts = rawAccounts
            .Select(a => (Dto: a, UserId: long.TryParse(a.UserId, out var id) ? id : 0L))
            .Where(a => a.UserId > 0)
            .ToList();

        // 2) 今天的执行记录：一次查完
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var records = await db
            .TaskRecords.Where(r => r.RecordDate == dateKey)
            .ToListAsync(cancellationToken);

        // 3) 每个 Job 今天有没有触发点、是否已过触发时间（读 Quartz）
        var dueInfo = await GetDueInfoAsync(now, cancellationToken);

        // 4) B 站状态：并发查（仅 includeBili 时）
        var profiles = new Dictionary<long, BiliAccountProbeResult>();
        var rewards = new Dictionary<long, (BiliDailyRewardSnapshot? Reward, bool Failed)>();
        if (includeBili && accounts.Count > 0)
        {
            var userIds = accounts.Select(a => a.UserId).ToList();
            var profileTask = accountProbe.ProbeManyAsync(userIds, forceRefresh, cancellationToken);
            var rewardTask = QueryBiliRewardsAsync(userIds, cancellationToken);
            await Task.WhenAll(profileTask, rewardTask);
            profiles = await profileTask;
            rewards = await rewardTask;
        }

        var result = new List<AccountTodayTasksDto>();

        foreach (var (account, userId) in accounts)
        {
            profiles.TryGetValue(userId, out var profile);

            var dto = new AccountTodayTasksDto
            {
                UserId = userId,
                UserName = profile?.UserName ?? $"账号 {account.Index + 1}",
                Index = account.Index,
                IsCookieValid = !includeBili || (profile?.Success ?? false),
                Groups = [],
            };

            rewards.TryGetValue(userId, out var reward);
            var biliQueryFailed = !includeBili || reward.Failed;
            var biliReward = includeBili ? reward.Reward : null;

            foreach (var task in TaskCatalog.All)
            {
                var taskRecords = records
                    .Where(r => r.UserId == userId && r.TaskKey == task.TaskKey)
                    .OrderBy(r => r.Id)
                    .ToList();

                var due = dueInfo.GetValueOrDefault(task.JobName);

                var group = new TodayTaskGroupDto
                {
                    DisplayName = task.DisplayName,
                    TaskKey = task.TaskKey,
                    Items = [],
                };

                foreach (var item in task.Items)
                {
                    // 传该任务今天的全部记录：每日任务的子项（登录/观看/分享/投币）定时执行时
                    // 只写任务级记录（TaskItemKey = null），子项级记录只在补做时产生。
                    var autoAttempts = taskRecords.Count(r =>
                        r.TaskItemKey == item.ItemKey && r.Trigger == TaskRecordTrigger.Auto
                    );

                    var isBiliItem = item.Source == TaskItemSource.BiliDailyReward;
                    var pending = isBiliItem && !includeBili;

                    var ctx = new TodayTaskItemContext
                    {
                        Task = task,
                        Item = item,
                        IsTaskEnabled = task.IsEnabled(configuration),
                        IsItemEnabled = item.IsEnabled(configuration),
                        HasFireTimeToday = due.HasFireTimeToday,
                        IsPastDueTime = due.IsPastDueTime,
                        BiliReward = biliReward,
                        BiliQueryFailed = isBiliItem && !pending && biliQueryFailed,
                        Records = taskRecords,
                        AutoAttempts = autoAttempts,
                        MaxAutoAttempts = MaxAutoAttempts,
                    };

                    var evaluated = TaskStatusEvaluator.Evaluate(ctx);

                    group.Items.Add(
                        new TodayTaskItemDto
                        {
                            ItemKey = item.ItemKey,
                            DisplayName = item.DisplayName,
                            State = evaluated.State,
                            StateText = pending ? "检测中" : Describe(evaluated.State),
                            Message = pending ? null : evaluated.Message,
                            CompletedAt = evaluated.CompletedAt,
                            AutoAttempts = evaluated.AutoAttempts,
                            AttemptedToday = taskRecords.Count > 0,
                            IsBiliPending = pending,
                            CanAutoRedo =
                                !pending && TaskStatusEvaluator.CanAutoRedo(ctx, evaluated),
                            CanDisableShare =
                                item.ItemKey == TaskCatalog.ShareItemKey
                                && item.IsEnabled(configuration),
                        }
                    );
                }

                dto.Groups.Add(group);
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<TaskRedoResultDto> RedoAsync(
        long userId,
        string taskKey,
        string? itemKey,
        TaskRecordTrigger trigger = TaskRecordTrigger.Manual,
        CancellationToken cancellationToken = default
    )
    {
        var task = TaskCatalog.All.FirstOrDefault(t => t.TaskKey == taskKey);
        if (task is null)
        {
            return new TaskRedoResultDto(false, $"未知任务：{taskKey}");
        }

        var item = task.Items.FirstOrDefault(i => i.ItemKey == itemKey);
        if (item is null)
        {
            return new TaskRedoResultDto(false, $"未知检查项：{itemKey}");
        }

        await RedoLock.WaitAsync(cancellationToken);
        try
        {
            return await ExecuteAndRecordAsync(userId, task, item, trigger, cancellationToken);
        }
        finally
        {
            RedoLock.Release();
        }
    }

    public async Task<int> RedoAllForAccountAsync(
        long userId,
        CancellationToken cancellationToken = default
    )
    {
        var status = await GetTodayStatusAsync(true, true, cancellationToken);
        var account = status.FirstOrDefault(a => a.UserId == userId);
        return account is null ? 0 : await RedoAccountAsync(account, cancellationToken);
    }

    public async Task<int> RedoAllMissingAsync(CancellationToken cancellationToken = default)
    {
        // 只查一次状态，避免逐账号重复请求 B 站接口
        var status = await GetTodayStatusAsync(true, true, cancellationToken);

        var count = 0;
        foreach (var account in status)
        {
            count += await RedoAccountAsync(account, cancellationToken);
        }

        return count;
    }

    public async Task DisableShareAsync(CancellationToken cancellationToken = default)
    {
        await SaveSettingsAsync(
            new Dictionary<string, string> { ["DailyTaskConfig:IsShareVideo"] = "false" }
        );
    }

    public async Task SaveAutoRecoverSettingsAsync(
        bool isEnable,
        int intervalHours,
        int retentionDays,
        CancellationToken cancellationToken = default
    )
    {
        await SaveSettingsAsync(
            new Dictionary<string, string>
            {
                ["AutoRecoverConfig:IsEnable"] = isEnable.ToString().ToLower(),
                ["AutoRecoverConfig:IntervalHours"] = Math.Clamp(intervalHours, 1, 24).ToString(),
                ["AutoRecoverConfig:RecordRetentionDays"] = Math.Clamp(retentionDays, 1, 90)
                    .ToString(),
            }
        );
    }

    #region private

    /// <summary>对已完成状态快照的账号执行全部可补做项</summary>
    private async Task<int> RedoAccountAsync(
        AccountTodayTasksDto account,
        CancellationToken cancellationToken
    )
    {
        var count = 0;
        foreach (var group in account.Groups)
        {
            foreach (var item in group.Items.Where(i => i.CanRedo))
            {
                var r = await RedoAsync(
                    account.UserId,
                    group.TaskKey,
                    item.ItemKey,
                    TaskRecordTrigger.Manual,
                    cancellationToken
                );
                count++;
                logger.LogInformation(
                    "补做 {user}/{task}/{item}：{result}",
                    account.UserId,
                    group.TaskKey,
                    item.ItemKey,
                    r.Message
                );
            }
        }

        return count;
    }

    /// <summary>并发查询多个账号的 B 站每日任务状态</summary>
    private async Task<
        Dictionary<long, (BiliDailyRewardSnapshot? Reward, bool Failed)>
    > QueryBiliRewardsAsync(List<long> userIds, CancellationToken cancellationToken)
    {
        var tasks = userIds.Select(async userId =>
            (UserId: userId, Result: await QueryBiliRewardAsync(userId, cancellationToken))
        );

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(x => x.UserId, x => x.Result);
    }

    /// <summary>
    /// 查询某账号的 B 站每日任务状态。返回 (快照, 是否查询失败)。
    /// Cookie 无效或接口异常时快照为 null 且标记失败 —— 页面显示「状态未知」，且不参与补做判定。
    /// </summary>
    private async Task<(BiliDailyRewardSnapshot? Reward, bool Failed)> QueryBiliRewardAsync(
        long userId,
        CancellationToken cancellationToken
    )
    {
        var ck = FindCookie(userId);
        if (ck is null)
        {
            return (null, true);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(BiliQueryTimeout);

        try
        {
            var info = await accountDomainService.GetDailyTaskStatus(ck);
            if (info is null)
            {
                return (null, true);
            }

            var donatedCoins = await coinDomainService.GetDonatedCoins(ck);

            return (
                new BiliDailyRewardSnapshot(info.Login, info.Watch, info.Share, donatedCoins * 10),
                false
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("查询账号 {uid} 的每日任务状态超时", userId);
            return (null, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "查询账号 {uid} 的每日任务状态失败", userId);
            return (null, true);
        }
    }

    private async Task<TaskRedoResultDto> ExecuteAndRecordAsync(
        long userId,
        TaskDefinition task,
        TaskItemDefinition item,
        TaskRecordTrigger trigger,
        CancellationToken cancellationToken
    )
    {
        string? error = null;
        try
        {
            await recoveryExecutor.ExecuteAsync(userId, task, item, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        // 记录写入交给 ITaskRecordWriter：它内部会吞掉写库异常。
        // 若在这里直接写库并把写入和「任务执行」放进同一个 try，写库失败会被当成任务失败。
        await recordWriter.WriteAsync(
            userId,
            task.TaskKey,
            item.ItemKey,
            error is null ? TaskRecordStatus.Success : TaskRecordStatus.Failed,
            error,
            trigger,
            cancellationToken
        );

        return error is null
            ? new TaskRedoResultDto(true, $"{item.DisplayName}：执行完成")
            : new TaskRedoResultDto(false, $"{item.DisplayName}：{error}");
    }

    /// <summary>每个任务今天有没有触发点、是否已过今天的最后一次触发时间</summary>
    private async Task<Dictionary<string, DueInfo>> GetDueInfoAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<string, DueInfo>();
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        foreach (var task in TaskCatalog.All)
        {
            var jobKey = new JobKey(task.JobName, Constants.BiliJobGroup);
            string? cron = null;
            try
            {
                var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);
                cron = triggers.OfType<ICronTrigger>().FirstOrDefault()?.CronExpressionString;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "读取任务 {job} 的触发器失败", task.JobName);
            }

            var fireTimes = TaskDueTimeCalculator.GetFireTimesOfDay(cron, now);
            result[task.JobName] = new DueInfo(
                fireTimes.Count > 0,
                TaskDueTimeCalculator.IsDue(cron, now)
            );
        }

        return result;
    }

    private BiliCookie? FindCookie(long userId)
    {
        for (int i = 0; i < cookieStrFactory.Count; i++)
        {
            var ck = cookieStrFactory.GetCookie(i);
            if (ck.UserId == userId.ToString())
            {
                return ck;
            }
        }

        return null;
    }

    private async Task SaveSettingsAsync(Dictionary<string, string> values)
    {
        if (configuration is not IConfigurationRoot root)
        {
            throw new Exception("无法获取配置根对象");
        }

        var provider = root.Providers.OfType<SqliteConfigurationProvider>().FirstOrDefault();
        if (provider is null)
        {
            throw new Exception("无法获取数据库配置提供器");
        }

        provider.BatchSet(values);
        root.Reload();

        // 间隔小时数变了要重建 Quartz 触发器
        await RescheduleAutoRecoverAsync();
    }

    /// <summary>
    /// 按当前配置重建自动补做的触发器（间隔小时数变了要重新调度）。
    /// </summary>
    private async Task RescheduleAutoRecoverAsync()
    {
        try
        {
            var intervalHours = Math.Clamp(
                configuration.GetValue("AutoRecoverConfig:IntervalHours", 2),
                1,
                24
            );
            var scheduler = await schedulerFactory.GetScheduler();
            var triggerKey = AutoRecoverJob.TriggerKeyValue;
            if (!await scheduler.CheckExists(triggerKey))
            {
                return;
            }

            var newTrigger = TriggerBuilder
                .Create()
                .WithIdentity(triggerKey)
                .ForJob(AutoRecoverJob.Key)
                .StartAt(DateTimeOffset.UtcNow.AddMinutes(1))
                .WithSimpleSchedule(x => x.WithIntervalInHours(intervalHours).RepeatForever())
                .Build();

            await scheduler.RescheduleJob(triggerKey, newTrigger);
        }
        catch (Exception ex)
        {
            // 重新调度失败不影响已保存的配置；下次重启会按新值启动
            logger.LogWarning(ex, "重建自动补做触发器失败");
        }
    }

    /// <inheritdoc />
    public async Task CleanupExpiredRecordsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default
    )
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

    private static string Describe(TodayTaskItemState state) =>
        state switch
        {
            TodayTaskItemState.Completed => "已完成",
            TodayTaskItemState.NotDone => "今天还没做",
            TodayTaskItemState.Failed => "失败",
            TodayTaskItemState.RetryExhausted => "已自动重试 3 次仍未完成",
            TodayTaskItemState.Waiting => "等待执行",
            TodayTaskItemState.NotToday => "本日无需执行",
            TodayTaskItemState.Disabled => "已关闭",
            TodayTaskItemState.Unknown => "状态未知",
            _ => state.ToString(),
        };

    private readonly record struct DueInfo(bool HasFireTimeToday, bool IsPastDueTime);

    #endregion private
}
