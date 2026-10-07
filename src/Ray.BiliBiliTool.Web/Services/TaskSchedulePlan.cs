using System.Globalization;
using Quartz;

namespace Ray.BiliBiliTool.Web.Services;

public enum SchedulePeriod
{
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Hours,
    Minutes,
}

public sealed class TaskSchedulePlan
{
    public const string DefaultCron = "0 0 0 1 1 ?";
    public static readonly int[] HourIntervals = [1, 2, 3, 4, 6, 8, 12];
    public static readonly int[] MinuteIntervals = [1, 2, 3, 5, 10, 15, 20, 30];
    public static readonly string[] WeekDays = ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];
    public SchedulePeriod Period { get; set; } = SchedulePeriod.Daily;
    public TimeSpan Time { get; set; } = TimeSpan.FromHours(8);
    public int Day { get; set; } = 1;
    public int Month { get; set; } = 1;
    public int Interval { get; set; } = 1;
    public HashSet<int> Days { get; set; } = [2];

    public string ToCron()
    {
        if (
            Time < TimeSpan.Zero
            || Time >= TimeSpan.FromDays(1)
            || Time.Ticks % TimeSpan.TicksPerSecond != 0
        )
            throw new FormatException("请选择有效的执行时间");
        var prefix = $"{Time.Seconds} {Time.Minutes} {Time.Hours}";
        var cron = Period switch
        {
            SchedulePeriod.Daily => $"{prefix} * * ?",
            SchedulePeriod.Weekly when Days.Count > 0 && Days.All(d => d is >= 1 and <= 7) =>
                $"{prefix} ? * {string.Join(',', Days.Order().Select(d => WeekDays[d - 1]))}",
            SchedulePeriod.Monthly when Day is >= 0 and <= 31 =>
                $"{prefix} {(Day == 0 ? "L" : Day.ToString(CultureInfo.InvariantCulture))} * ?",
            SchedulePeriod.Yearly
                when Month is >= 1 and <= 12
                    && Day >= 1
                    && Day <= DateTime.DaysInMonth(2000, Month) => $"{prefix} {Day} {Month} ?",
            SchedulePeriod.Hours when HourIntervals.Contains(Interval) =>
                $"{Time.Seconds} {Time.Minutes} 0/{Interval} * * ?",
            SchedulePeriod.Minutes when MinuteIntervals.Contains(Interval) =>
                $"{Time.Seconds} 0/{Interval} * * * ?",
            _ => throw new FormatException(
                Period == SchedulePeriod.Weekly ? "请至少选择一天" : "请选择有效的执行日期或间隔"
            ),
        };
        _ = new CronExpression(cron);
        return cron;
    }

    public string Summary =>
        Period switch
        {
            SchedulePeriod.Daily => $"每天 {TimeText}",
            SchedulePeriod.Weekly =>
                $"每周{string.Join('、', Days.OrderBy(d => d == 1 ? 8 : d).Select(d => "日一二三四五六"[d - 1]))} {TimeText}",
            SchedulePeriod.Monthly => $"每月{(Day == 0 ? "最后一天" : $" {Day} 日")} {TimeText}",
            SchedulePeriod.Yearly => $"每年 {Month} 月 {Day} 日 {TimeText}",
            SchedulePeriod.Hours => $"每隔 {Interval} 小时，在第 {Time.Minutes} 分{SecondText}执行",
            _ => $"每隔 {Interval} 分钟，在第 {Time.Seconds} 秒执行",
        };
    public string TimeText =>
        Time.ToString(Time.Seconds == 0 ? @"hh\:mm" : @"hh\:mm\:ss", CultureInfo.InvariantCulture);
    private string SecondText => Time.Seconds == 0 ? "" : $" {Time.Seconds} 秒";

    public static bool TryParse(string? value, out TaskSchedulePlan plan)
    {
        plan = new();
        var expression = string.IsNullOrWhiteSpace(value) ? DefaultCron : value;
        try
        {
            _ = new CronExpression(expression);
        }
        catch (FormatException)
        {
            return false;
        }
        var f = expression
            .ToUpperInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (f.Length is < 6 or > 7 || (f.Length == 7 && f[6] != "*"))
            return false;
        if (!Number(f[0], 0, 59, out var second))
            return false;
        if (f[3] == "*" && f[4] == "*" && f[5] == "?")
        {
            if (Step(f[1], MinuteIntervals, out var minutes) && f[2] == "*")
            {
                plan.Period = SchedulePeriod.Minutes;
                plan.Interval = minutes;
                plan.Time = new(0, 0, second);
                return true;
            }
            if (Number(f[1], 0, 59, out var minute) && Step(f[2], HourIntervals, out var hours))
            {
                plan.Period = SchedulePeriod.Hours;
                plan.Interval = hours;
                plan.Time = new(0, minute, second);
                return true;
            }
        }
        if (!Number(f[1], 0, 59, out var min) || !Number(f[2], 0, 23, out var hour))
            return false;
        plan.Time = new(hour, min, second);
        if (f[3] == "*" && f[4] == "*" && f[5] == "?")
        {
            plan.Period = SchedulePeriod.Daily;
            return true;
        }
        if (f[3] == "?" && f[4] == "*")
        {
            var days = new HashSet<int>();
            foreach (var token in f[5].Split(','))
            {
                var range = token.Split('-');
                if (range.Length > 2 || !WeekDay(range[0], out var start))
                    return false;
                var end = start;
                if (range.Length == 2 && (!WeekDay(range[1], out end) || end < start))
                    return false;
                for (var d = start; d <= end; d++)
                    days.Add(d);
            }
            plan.Period = SchedulePeriod.Weekly;
            plan.Days = days;
            return days.Count > 0;
        }
        if (f[5] != "?")
            return false;
        if (f[4] == "*" && (f[3] == "L" || Number(f[3], 1, 31, out _)))
        {
            plan.Period = SchedulePeriod.Monthly;
            plan.Day = f[3] == "L" ? 0 : int.Parse(f[3], CultureInfo.InvariantCulture);
            return true;
        }
        if (
            Number(f[4], 1, 12, out var month)
            && Number(f[3], 1, DateTime.DaysInMonth(2000, month), out var day)
        )
        {
            plan.Period = SchedulePeriod.Yearly;
            plan.Month = month;
            plan.Day = day;
            return true;
        }
        return false;
    }

    private static bool Number(string value, int min, int max, out int result) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result)
        && result >= min
        && result <= max;

    private static bool Step(string value, int[] allowed, out int result)
    {
        result = 0;
        var parts = value.Split('/');
        return parts.Length == 2
            && (parts[0] == "0" || parts[0] == "*")
            && Number(parts[1], 1, 60, out result)
            && allowed.Contains(result);
    }

    private static bool WeekDay(string value, out int day)
    {
        day = Array.IndexOf(WeekDays, value) + 1;
        return day > 0 || Number(value, 1, 7, out day);
    }
}
