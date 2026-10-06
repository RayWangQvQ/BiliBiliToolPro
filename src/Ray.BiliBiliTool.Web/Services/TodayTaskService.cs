using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
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
    ILogger<TodayTaskService> logger,
    ILiveMedalDashboardService liveMedals,
    IServiceScopeFactory? recoveryScopeFactory = null,
    TimeProvider? clock = null
) : ITodayTaskService
{
    /// <summary>自动补做次数上限</summary>
    public const int MaxAutoAttempts = 3;

    // Serialize all recovery items for the same account and task.
    private static readonly ConcurrentDictionary<
        (long UserId, string Task),
        SemaphoreSlim
    > RedoLocks = new();

    private static readonly TimeSpan BiliQueryTimeout = TimeSpan.FromSeconds(20);

    public async Task<List<AccountTodayTasksDto>> GetTodayStatusAsync(
        bool includeBili,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        var now = (clock ?? TimeProvider.System).GetLocalNow();
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
        var medalOptions =
            configuration.GetSection("LiveFansMedalTaskConfig").Get<LiveFansMedalTaskOptions>()
            ?? new();
        var medalProgress = new Dictionary<long, LiveMedalCompletion>();
        var profiles = new Dictionary<long, BiliAccountProbeResult>();
        var rewards = new Dictionary<long, (BiliDailyRewardSnapshot? Reward, bool Failed)>();
        if (includeBili && accounts.Count > 0)
        {
            var userIds = accounts.Select(a => a.UserId).ToList();
            var profileTask = accountProbe.ProbeManyAsync(userIds, forceRefresh, cancellationToken);
            var rewardTask = QueryBiliRewardsAsync(userIds, cancellationToken);
            var medalTask = QueryMedalsAsync(
                accounts.Select(account => (account.UserId, account.Dto.Index)),
                medalOptions,
                forceRefresh,
                cancellationToken
            );
            await Task.WhenAll(profileTask, rewardTask, medalTask);
            medalProgress = await medalTask;
            profiles = await profileTask;
            rewards = await rewardTask;
        }

        var donationOptions =
            configuration.GetSection("DailyTaskConfig").Get<DailyTaskOptions>() ?? new();
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
                    var isMedalItem = item.Source == TaskItemSource.LiveMedalProgress;
                    var monitoredMedal = isMedalItem && medalOptions.UseLiveStateMonitoring;
                    var pending =
                        (isBiliItem || isMedalItem)
                        && !includeBili
                        && task.IsEnabled(configuration)
                        && item.IsEnabled(configuration)
                        && (monitoredMedal || due.HasFireTimeToday)
                        && (monitoredMedal || due.IsPastDueTime);

                    var ctx = new TodayTaskItemContext
                    {
                        Task = task,
                        Item = item,
                        IsTaskEnabled = task.IsEnabled(configuration),
                        IsItemEnabled =
                            item.IsEnabled(configuration)
                            && (
                                item.ItemKey != "DonateCoin"
                                || !donationOptions.ShouldSkipCoinDonation(profile?.Level)
                            ),
                        HasFireTimeToday = monitoredMedal || due.HasFireTimeToday,
                        IsPastDueTime = monitoredMedal || due.IsPastDueTime,
                        BiliReward = biliReward,
                        CoinDonationTarget = donationOptions.NumberOfCoins,
                        LiveMedal = medalProgress.GetValueOrDefault(userId),
                        FollowMedalDailyTaskLimit =
                            medalOptions.UseLiveStateMonitoring
                            || medalOptions.FollowDailyTaskLimit,
                        MonitorMedalLiveState = monitoredMedal,
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
                            StateText =
                                pending ? "检测中"
                                : isMedalItem && evaluated.State == TodayTaskItemState.NotDone
                                    ? "未完成"
                                : Describe(evaluated.State),
                            ProgressSummary = isMedalItem && !pending ? evaluated.Message : null,
                            Message = pending ? null : evaluated.Message,
                            CompletedAt = evaluated.CompletedAt,
                            AutoAttempts = evaluated.AutoAttempts,
                            AttemptedToday = taskRecords.Count > 0,
                            IsBiliPending = pending,
                            CanAutoRedo =
                                !pending
                                && !monitoredMedal
                                && TaskStatusEvaluator.CanAutoRedo(ctx, evaluated),
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
        using var notificationScope = new TaskFailureNotificationScope(suppress: true);
        using var watchScope = new LiveFansMedalWatchScope(trigger == TaskRecordTrigger.Manual);
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

        using var progressScope = new TaskRecoveryProgressScope(
            $"{userId}/{taskKey}/{itemKey}",
            userId
        );
        TaskRecoveryProgressScope.Report(
            "task",
            item.DisplayName,
            TaskRecoveryProgressState.Waiting,
            "等待该账号的同类补做结束"
        );
        var redoLock = RedoLocks.GetOrAdd((userId, taskKey), _ => new(1, 1));
        await redoLock.WaitAsync(cancellationToken);
        try
        {
            TaskRecoveryProgressScope.Report(
                "task",
                item.DisplayName,
                TaskRecoveryProgressState.Running,
                "正在检查账号并执行任务"
            );
            var result =
                await CheckRecoveryEligibilityAsync(userId, task, item, trigger, cancellationToken)
                ?? await ExecuteAndRecordAsync(userId, task, item, trigger, cancellationToken);
            TaskRecoveryProgressScope.Report(
                "task",
                item.DisplayName,
                result.Skipped ? TaskRecoveryProgressState.Skipped
                    : result.Success ? TaskRecoveryProgressState.Completed
                    : progressScope.HasFailures ? TaskRecoveryProgressState.Failed
                    : TaskRecoveryProgressState.Pending,
                result.Message
            );
            return result;
        }
        finally
        {
            redoLock.Release();
        }
    }

    public async Task<int> RedoAllForAccountAsync(
        long userId,
        CancellationToken cancellationToken = default
    )
    {
        using var notificationScope = new TaskFailureNotificationScope(suppress: true);
        TaskRecoveryProgressScope.Report(
            "prepare",
            "读取漏做任务",
            TaskRecoveryProgressState.Running,
            "正在读取当前账号的配置和 B 站今日进度"
        );
        var status = await GetTodayStatusAsync(true, true, cancellationToken);
        TaskRecoveryProgressScope.Report(
            "prepare",
            "读取漏做任务",
            TaskRecoveryProgressState.Completed,
            "已读取今日任务状态"
        );
        var account = status.FirstOrDefault(a => a.UserId == userId);
        if (account is null)
            return 0;
        var total = account.Groups.SelectMany(group => group.Items).Count(item => item.CanRedo);
        TaskRecoveryProgressScope.Report(
            "batch",
            "本轮补做",
            TaskRecoveryProgressState.Running,
            "正在处理当前账号的漏做项",
            0,
            total,
            "项"
        );
        return await RedoAccountsAsync([account], cancellationToken, total);
    }

    public async Task<int> RedoAllMissingAsync(CancellationToken cancellationToken = default)
    {
        using var notificationScope = new TaskFailureNotificationScope(suppress: true);
        TaskRecoveryProgressScope.Report(
            "prepare",
            "读取漏做任务",
            TaskRecoveryProgressState.Running,
            "正在读取全部账号的配置和 B 站今日进度"
        );
        // 只查一次状态，避免逐账号重复请求 B 站接口
        var status = await GetTodayStatusAsync(true, true, cancellationToken);
        TaskRecoveryProgressScope.Report(
            "prepare",
            "读取漏做任务",
            TaskRecoveryProgressState.Completed,
            "已读取今日任务状态"
        );
        var total = status
            .SelectMany(account => account.Groups)
            .SelectMany(group => group.Items)
            .Count(item => item.CanRedo);
        TaskRecoveryProgressScope.Report(
            "batch",
            "本轮补做",
            TaskRecoveryProgressState.Running,
            "正在处理全部账号的漏做项",
            0,
            total,
            "项"
        );

        return await RedoAccountsAsync(status, cancellationToken, total);
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

    private async Task<TaskRedoResultDto?> CheckRecoveryEligibilityAsync(
        long userId,
        TaskDefinition task,
        TaskItemDefinition item,
        TaskRecordTrigger trigger,
        CancellationToken token
    )
    {
        TaskRedoResultDto Skip(string message, bool complete = false) =>
            new(complete, $"{item.DisplayName}：{message}", Skipped: true);

        if (!task.IsEnabled(configuration) || !item.IsEnabled(configuration))
            return Skip("已关闭，跳过补做");
        var cookie = FindCookie(userId);
        if (cookie is null)
            return Skip("账号已移除，跳过补做");
        var options =
            configuration.GetSection("LiveFansMedalTaskConfig").Get<LiveFansMedalTaskOptions>()
            ?? new();
        if (trigger == TaskRecordTrigger.Auto)
        {
            if (!configuration.GetValue("AutoRecoverConfig:IsEnable", true))
                return Skip("自动补做已关闭");
            if (
                item.ItemKey == TaskCatalog.ShareItemKey
                || (
                    item.Source == TaskItemSource.LiveMedalProgress
                    && options.UseLiveStateMonitoring
                )
            )
                return Skip("此项不参与自动补做");
        }

        try
        {
            List<TaskRecord> records = [];
            if (item.Source == TaskItemSource.ExecutionRecord || trigger == TaskRecordTrigger.Auto)
            {
                await using var db = await dbContextFactory.CreateDbContextAsync(token);
                var date = DateTimeOffset.Now.ToString("yyyy-MM-dd");
                records = await db
                    .TaskRecords.AsNoTracking()
                    .Where(r =>
                        r.UserId == userId
                        && r.TaskKey == task.TaskKey
                        && r.RecordDate == date
                        && (r.TaskItemKey == item.ItemKey || r.TaskItemKey == null)
                    )
                    .ToListAsync(token);
                if (
                    trigger == TaskRecordTrigger.Auto
                    && records.Count(r =>
                        r.TaskItemKey == item.ItemKey && r.Trigger == TaskRecordTrigger.Auto
                    ) >= MaxAutoAttempts
                )
                    return Skip($"今日自动补做已达到 {MaxAutoAttempts} 次");
            }
            if (
                item.Source == TaskItemSource.ExecutionRecord
                && records.Any(r => r.Status == TaskRecordStatus.Success)
            )
                return Skip("今日已完成，无需补做", true);
            if (item.Source == TaskItemSource.BiliDailyReward)
            {
                var (reward, failed) = await QueryBiliRewardAsync(userId, token);
                if (failed || reward is null)
                    return Skip("今日进度暂未获取，请稍后重试");
                if (IsDailyItemComplete(item, reward))
                    return Skip("B 站已确认今日完成，无需补做", true);
                if (item.ItemKey == "DonateCoin")
                {
                    var donation =
                        configuration.GetSection("DailyTaskConfig").Get<DailyTaskOptions>()
                        ?? new();
                    if (donation.EffectiveCoinDonationStopLevel > 0)
                    {
                        var account = await accountDomainService
                            .LoginByCookie(cookie)
                            .WaitAsync(token);
                        if (donation.ShouldSkipCoinDonation(account.Level_info?.Current_level))
                            return Skip("已达到停止投币等级");
                    }
                }
            }
            if (item.Source == TaskItemSource.LiveMedalProgress)
            {
                var index = Enumerable
                    .Range(0, cookieStrFactory.Count)
                    .First(i => cookieStrFactory.GetCookie(i).UserId == cookie.UserId);
                var progress = (await QueryMedalsAsync([(userId, index)], options, true, token))[
                    userId
                ];
                if (progress.State != TodayTaskItemState.NotDone)
                    return Skip(
                        progress.Message ?? "当前无需补做",
                        progress.State == TodayTaskItemState.Completed
                    );
                if (
                    trigger == TaskRecordTrigger.Auto
                    && !options.FollowDailyTaskLimit
                    && records.Any(r => r.Status == TaskRecordStatus.Success)
                )
                    return Skip("今日设置的执行目标已执行");
            }
            // Re-read switches after platform queries, which may take several seconds.
            if (
                !task.IsEnabled(configuration)
                || !item.IsEnabled(configuration)
                || (
                    trigger == TaskRecordTrigger.Auto
                    && !configuration.GetValue("AutoRecoverConfig:IsEnable", true)
                )
            )
                return Skip("配置已关闭，跳过补做");
            token.ThrowIfCancellationRequested();
            return null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Recovery eligibility check failed: {ErrorType}", ex.GetType().Name);
            return Skip("补做条件暂未核实，请稍后重试");
        }
    }

    private bool IsDailyItemComplete(TaskItemDefinition item, BiliDailyRewardSnapshot reward) =>
        item.ItemKey switch
        {
            "Login" => reward.Login,
            "Watch" => reward.Watch,
            TaskCatalog.ShareItemKey => reward.Share,
            "DonateCoin" => TaskStatusEvaluator.IsCoinDonationComplete(
                reward.CoinExp,
                configuration.GetValue("DailyTaskConfig:NumberOfCoins", 5)
            ),
            _ => false,
        };

    /// <summary>Runs eligible recovery items for accounts with a loaded status snapshot.</summary>
    private async Task<int> RedoAccountsAsync(
        IReadOnlyList<AccountTodayTasksDto> accounts,
        CancellationToken cancellationToken,
        int total
    )
    {
        var count = 0;
        var executed = 0;
        var progressGate = new object();
        var groups = accounts
            .SelectMany(account => account.Groups.Select(group => (account, group)))
            .OrderBy(entry => entry.group.TaskKey == "LiveFansMedalAppService");
        await Parallel.ForEachAsync(
            groups,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken,
            },
            async (entry, token) =>
            {
                var (account, group) = entry;
                foreach (var item in group.Items.Where(i => i.CanRedo))
                {
                    var r = await RedoAsync(
                        account.UserId,
                        group.TaskKey,
                        item.ItemKey,
                        TaskRecordTrigger.Manual,
                        token
                    );
                    if (!r.Skipped)
                        Interlocked.Increment(ref executed);
                    lock (progressGate)
                    {
                        count++;
                        TaskRecoveryProgressScope.Report(
                            "batch",
                            "本轮补做",
                            count == total
                                ? TaskRecoveryProgressState.Completed
                                : TaskRecoveryProgressState.Running,
                            "已处理的任务结果见下方",
                            count,
                            total,
                            "项"
                        );
                    }
                    logger.LogInformation(
                        "补做 {user}/{task}/{item}：{result}",
                        account.UserId,
                        group.TaskKey,
                        item.ItemKey,
                        r.Message
                    );
                }
            }
        );

        return executed;
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
            var info = await accountDomainService.GetDailyTaskStatus(ck).WaitAsync(timeout.Token);
            if (info is null)
            {
                return (null, true);
            }

            var donatedCoins = await coinDomainService.GetDonatedCoins(ck).WaitAsync(timeout.Token);

            return (
                new BiliDailyRewardSnapshot(info.Login, info.Watch, info.Share, donatedCoins * 10),
                false
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
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

    private async Task<Dictionary<long, LiveMedalCompletion>> QueryMedalsAsync(
        IEnumerable<(long UserId, int Index)> accounts,
        LiveFansMedalTaskOptions options,
        bool forceRefresh,
        CancellationToken token
    )
    {
        if (!options.IsEnable)
            return [];
        var results = await Task.WhenAll(
            accounts.Select(async account =>
            {
                LiveMedalSnapshot? snapshot = null;
                try
                {
                    snapshot = await liveMedals.GetAsync(account.Index, forceRefresh, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Medal status read failed: {ErrorType}", ex.GetType().Name);
                }
                return (
                    account.UserId,
                    Result: LiveMedalCompletionEvaluator.Evaluate(
                        snapshot,
                        options,
                        DateTimeOffset.UtcNow
                    )
                );
            })
        );
        return results.ToDictionary(result => result.UserId, result => result.Result);
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
            // Domain services may contain per-run caches. Keep concurrent accounts isolated.
            using var executionScope = recoveryScopeFactory?.CreateScope();
            var executor =
                executionScope?.ServiceProvider.GetRequiredService<TaskRecoveryExecutor>()
                ?? recoveryExecutor;
            await executor.ExecuteAsync(userId, task, item, cancellationToken);
        }
        catch (TaskRecoverySkippedException ex)
        {
            return new(false, $"{item.DisplayName}：{ex.Message}", Skipped: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            error = TaskRecoveryProgressScope.DescribeFailure(ex);
            TaskRecoveryProgressScope.Report(
                "task",
                item.DisplayName,
                TaskRecoveryProgressState.Failed,
                error
            );
        }

        async Task<TaskRedoResultDto> FinishAsync(TaskRedoResultDto result)
        {
            // Persist success only after the platform confirms progress-backed tasks.
            await recordWriter.WriteAsync(
                userId,
                task.TaskKey,
                item.ItemKey,
                error is not null ? TaskRecordStatus.Failed
                    : result.Success ? TaskRecordStatus.Success
                    : TaskRecordStatus.Pending,
                result.Success ? null : error ?? result.Message,
                trigger,
                cancellationToken
            );
            return result;
        }

        if (error is null && item.Source == TaskItemSource.LiveMedalProgress)
        {
            TaskRecoveryProgressScope.Report(
                "task",
                item.DisplayName,
                TaskRecoveryProgressState.Running,
                "正在读取 B 站确认的今日进度"
            );
            var options =
                configuration.GetSection("LiveFansMedalTaskConfig").Get<LiveFansMedalTaskOptions>()
                ?? new();
            var index = Enumerable
                .Range(0, cookieStrFactory.Count)
                .FirstOrDefault(
                    index => cookieStrFactory.GetCookie(index).UserId == userId.ToString(),
                    -1
                );
            if (index < 0)
                return await FinishAsync(new(false, "粉丝牌进度暂未获取，请刷新"));
            var results = await QueryMedalsAsync(
                [(userId, index)],
                options,
                true,
                cancellationToken
            );
            var completion = results.GetValueOrDefault(userId);
            return await FinishAsync(
                new(
                    completion?.State == TodayTaskItemState.Completed,
                    $"{item.DisplayName}：{completion?.Message ?? "请刷新查看今日进度"}"
                )
            );
        }

        if (error is null && item.Source == TaskItemSource.BiliDailyReward)
        {
            TaskRecoveryProgressScope.Report(
                "task",
                item.DisplayName,
                TaskRecoveryProgressState.Running,
                "正在读取 B 站确认的今日进度"
            );
            var (reward, failed) = await QueryBiliRewardAsync(userId, cancellationToken);
            if (failed || reward is null)
                return await FinishAsync(
                    new(false, $"{item.DisplayName}：动作已执行，今日进度暂未获取")
                );
            var complete = IsDailyItemComplete(item, reward);
            return await FinishAsync(
                new(
                    complete,
                    $"{item.DisplayName}：{(complete ? "B 站已确认完成" : "动作已执行，B 站尚未确认完成")}"
                )
            );
        }

        return await FinishAsync(
            error is null
                ? new TaskRedoResultDto(true, $"{item.DisplayName}：执行完成")
                : new TaskRedoResultDto(false, $"{item.DisplayName}：{error}")
        );
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
            if (!await scheduler.Exists(triggerKey))
            {
                return;
            }

            var newTrigger = TriggerBuilder
                .Create()
                .WithIdentity(triggerKey)
                .ForJob(AutoRecoverJob.Key)
                .StartAt(DateTimeOffset.UtcNow.AddMinutes(1))
                .WithSimpleSchedule(x =>
                    x.WithInterval(TimeSpan.FromHours(intervalHours)).RepeatForever()
                )
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
            TodayTaskItemState.NoWork => "当前无需执行",
            TodayTaskItemState.WaitingWatchTime => "等待观看时段",
            TodayTaskItemState.WaitingConditions => "等待任务条件",
            TodayTaskItemState.NotToday => "本日无需执行",
            TodayTaskItemState.Disabled => "已关闭",
            TodayTaskItemState.Unknown => "状态未知",
            _ => state.ToString(),
        };

    private readonly record struct DueInfo(bool HasFireTimeToday, bool IsPastDueTime);

    #endregion private
}
