using Microsoft.EntityFrameworkCore;
using Quartz;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record DailyTaskNotificationItem(
    string TaskKey,
    string DisplayName,
    IReadOnlyList<string> Accounts,
    IReadOnlyList<string> PendingAccounts
);

public sealed record DailyTaskNotificationStatus(
    bool AllFinished,
    bool Running,
    IReadOnlyList<DailyTaskNotificationItem> Items
);

public interface IDailyTaskNotificationStatusSource
{
    Task<DailyTaskNotificationStatus> ReadAsync(
        DateOnly day,
        DateTimeOffset now,
        CancellationToken token
    );
}

public static class DailyTaskNotificationSchedule
{
    public const string CutoffKey = "TaskFailureNotification:DailySummaryTime";
    public const string DefaultTime = "23:55";

    public static TimeSpan Parse(string? value) =>
        TimeSpan.TryParseExact(
            value,
            @"hh\:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            out var time
        )
        && time >= TimeSpan.Zero
        && time < TimeSpan.FromDays(1)
            ? time
            : TimeSpan.Parse(DefaultTime);

    public static DateOnly Day(DateTimeOffset now) =>
        DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8)).Date);

    public static string Mask(long userId)
    {
        var value = userId.ToString();
        return "***" + value[^Math.Min(4, value.Length)..];
    }
}

public sealed class DailyTaskNotificationStatusSource(IServiceScopeFactory scopes)
    : IDailyTaskNotificationStatusSource
{
    public async Task<DailyTaskNotificationStatus> ReadAsync(
        DateOnly day,
        DateTimeOffset now,
        CancellationToken token
    )
    {
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var config = services.GetRequiredService<IConfiguration>();
        var cookies = services.GetRequiredService<CookieStrFactory<BiliCookie>>();
        var accounts = Enumerable
            .Range(0, cookies.Count)
            .Select(index =>
                (
                    Index: index,
                    UserId: long.TryParse(cookies.GetCookie(index).UserId, out var id) ? id : 0
                )
            )
            .Where(account => account.UserId > 0)
            .ToArray();
        var factory = services.GetRequiredService<IDbContextFactory<BiliDbContext>>();
        await using var db = await factory.CreateDbContextAsync(token);
        var dateKey = day.ToString("yyyy-MM-dd");
        var records = await db
            .TaskRecords.Where(record =>
                record.RecordDate == dateKey && record.Trigger == TaskRecordTrigger.Scheduled
            )
            .ToListAsync(token);
        var scheduler = await services.GetRequiredService<ISchedulerFactory>().GetScheduler(token);
        var runningJobs = (
            await scheduler.QueryFireInstances(
                new FireInstanceQuery { Take = PagedQuery.All },
                token
            )
        ).Items;
        var medalOptions =
            config.GetSection("LiveFansMedalTaskConfig").Get<LiveFansMedalTaskOptions>() ?? new();
        var items = new List<DailyTaskNotificationItem>();
        var allFinished = true;
        var running = false;
        var date = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(8));
        foreach (
            var task in TaskCatalog.All.Where(task =>
                task.IsEnabled(config) && task.Items.Any(item => item.IsEnabled(config))
            )
        )
        {
            var monitored =
                task.TaskKey == "LiveFansMedalAppService" && medalOptions.UseLiveStateMonitoring;
            var key = new JobKey(task.JobName, Constants.BiliJobGroup);
            var triggers = await scheduler.GetTriggersOfJob(key, token);
            var fires = triggers
                .OfType<ICronTrigger>()
                .SelectMany(trigger =>
                    TaskDueTimeCalculator.GetFireTimesOfDay(trigger.CronExpressionString, date)
                )
                .ToArray();
            if (!monitored && fires.Length == 0)
                continue;
            var active = runningJobs.Any(job =>
                job.JobKey.Equals(key)
                && triggers
                    .OfType<ICronTrigger>()
                    .Any(trigger => trigger.Key.Equals(job.TriggerKey))
            );
            if (monitored)
                active |= services.GetRequiredService<LiveMedalMonitorCycle>().ActiveCount > 0;
            running |= active;
            var pending = new List<string>();
            foreach (var account in accounts)
            {
                var finished =
                    !active
                    && (
                        monitored
                            ? await MedalFinishedAsync(
                                services,
                                account.Index,
                                account.UserId,
                                medalOptions,
                                day,
                                now,
                                token
                            )
                            : ScheduledFinished(
                                records,
                                account.UserId,
                                task.TaskKey,
                                fires.Max(),
                                now
                            )
                    );
                if (!finished)
                    pending.Add(DailyTaskNotificationSchedule.Mask(account.UserId));
            }
            allFinished &= pending.Count == 0;
            items.Add(
                new(
                    task.TaskKey,
                    task.DisplayName,
                    accounts
                        .Select(account => DailyTaskNotificationSchedule.Mask(account.UserId))
                        .ToArray(),
                    pending
                )
            );
        }
        return new(accounts.Length > 0 && allFinished && !running, running, items);
    }

    public static bool ScheduledFinished(
        IReadOnlyList<TaskRecord> records,
        long account,
        string taskKey,
        DateTimeOffset lastFire,
        DateTimeOffset now
    ) =>
        now >= lastFire
        && records
            .Where(record =>
                record.UserId == account
                && record.TaskKey == taskKey
                && record.Trigger == TaskRecordTrigger.Scheduled
                && record.TaskItemKey is null
                && record.CreatedAtUtc >= lastFire
            )
            .OrderBy(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id)
            .LastOrDefault()
            ?.Status == TaskRecordStatus.Success;

    private static async Task<bool> MedalFinishedAsync(
        IServiceProvider services,
        int index,
        long userId,
        LiveFansMedalTaskOptions options,
        DateOnly day,
        DateTimeOffset now,
        CancellationToken token
    )
    {
        // Read the existing progress cache; notification checks do not call Bilibili.
        var snapshot = await services
            .GetRequiredService<ILiveMedalDashboardService>()
            .GetCachedAsync(index, token);
        var gate = services.GetRequiredService<LiveFansMedalExecutionGate>();
        return MedalFinished(snapshot, options, userId, day, now, gate);
    }

    public static bool MedalFinished(
        LiveMedalSnapshot? snapshot,
        LiveFansMedalTaskOptions options,
        long userId,
        DateOnly day,
        DateTimeOffset now,
        LiveFansMedalExecutionGate gate
    )
    {
        if (!options.IsEnable || !options.UseLiveStateMonitoring)
            return true;
        var excluded = options.GetExcludedAnchorIds();
        var included = options.GetIncludedAnchorIds();
        if (options.OnlySelectedAnchors && included.Count == 0)
            return true;
        if (
            !new[] { "like", "sendDanmu", "watchLive" }.Any(action =>
                LiveMedalCompletionEvaluator.IsActionEnabled(action, options)
            )
        )
            return true;
        if (
            snapshot is null
            || snapshot.Error is not null
            || DailyTaskNotificationSchedule.Day(snapshot.UpdatedAt) != day
            || now - snapshot.UpdatedAt
                > TimeSpan.FromMinutes(Math.Max(5, options.MonitorIntervalMinutes + 2))
            || snapshot.UpdatedAt - now > TimeSpan.FromMinutes(1)
        )
            return false;
        foreach (
            var card in snapshot.Medals.Where(card =>
                card.CanInteract
                && !excluded.Contains(card.AnchorId)
                && (!options.OnlySelectedAnchors || included.Contains(card.AnchorId))
            )
        )
        {
            var observedAt = card.ProgressUpdatedAt ?? snapshot.UpdatedAt;
            if (
                DailyTaskNotificationSchedule.Day(observedAt) != day
                || now - observedAt
                    > TimeSpan.FromMinutes(Math.Max(5, options.MonitorIntervalMinutes + 2))
                || observedAt - now > TimeSpan.FromMinutes(1)
            )
                return false;
            if (card.Error is not null || card.Lighted is null)
                return false;
            if (card.Lighted == true && card.SavingsFull)
                continue;
            var tasks = card
                .Tasks.Where(task =>
                    LiveMedalCompletionEvaluator.IsActionEnabled(task.Action, options)
                )
                .ToArray();
            if (tasks.Length == 0)
                return false;
            foreach (var task in tasks)
            {
                if (
                    task.Done
                    || (LiveMedalCompletionEvaluator.IsLighting(task) && card.Lighted == true)
                )
                    continue;
                var plan = task.GetPlan(card.Lighted, card.SavingsFull);
                if (plan is null)
                    return false;
                if (
                    plan.Remaining > 0
                    && gate.Remaining(
                        userId.ToString(),
                        card.AnchorId,
                        task.Action,
                        options.GetInteractionLimit(task.Action),
                        plan.Completed
                    ) > 0
                )
                    return false;
            }
        }
        return true;
    }
}
