using Quartz;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 计算任务「今天该做的时间点」，用于判断是否已经到点 —— 自动补做不能抢在计划时间之前执行。
/// 纯逻辑，无 IO，便于单测。
/// </summary>
public static class TaskDueTimeCalculator
{
    /// <summary>触发时间过后多久才算「真的漏了」</summary>
    public static readonly TimeSpan DefaultGrace = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 枚举 cron 在指定日期这一天的全部触发时间（升序）。cron 为空/非法时返回空列表。
    /// </summary>
    public static List<DateTimeOffset> GetFireTimesOfDay(string? cron, DateTimeOffset day)
    {
        var result = new List<DateTimeOffset>();
        if (string.IsNullOrWhiteSpace(cron) || !CronExpression.IsValidExpression(cron))
        {
            return result;
        }

        var expression = new CronExpression(cron);
        var midnight = new DateTimeOffset(day.Year, day.Month, day.Day, 0, 0, 0, day.Offset);

        // 起点回退 1 秒：Quartz 的 GetNextValidTimeAfter 返回「严格晚于」的时间，
        // 不回退就会漏掉正好落在当天 00:00:00 的那一次触发。
        var start = midnight.AddSeconds(-1);
        var end = midnight.AddDays(1);

        var cursor = expression.GetNextValidTimeAfter(start);
        while (cursor.HasValue && cursor.Value < end)
        {
            result.Add(cursor.Value);
            cursor = expression.GetNextValidTimeAfter(cursor.Value);
        }

        return result;
    }

    /// <summary>
    /// 该任务今天是否已经到点。今天没有任何触发点 → 返回 false（本日无需执行）。
    /// </summary>
    public static bool IsDue(string? cron, DateTimeOffset now, TimeSpan? grace = null)
    {
        var fireTimes = GetFireTimesOfDay(cron, now);
        if (fireTimes.Count == 0)
        {
            return false;
        }

        return now >= fireTimes[^1] + (grace ?? DefaultGrace);
    }
}
