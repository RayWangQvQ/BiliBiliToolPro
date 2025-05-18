using BlazingQuartz;
using BlazingQuartz.Core.Models;
using MudBlazor;
using Quartz;
using IntervalUnit = BlazingQuartz.IntervalUnit;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class ModelExtensions
{
    public static string GetTriggerTypeIcon(this TriggerType triggerType)
    {
        switch (triggerType)
        {
            case TriggerType.Cron:
                return Icons.Material.Filled.Schedule;
            case TriggerType.Daily:
                return Icons.Material.Filled.Alarm;
            case TriggerType.Simple:
                return Icons.Material.Filled.Repeat;
            case TriggerType.Calendar:
                return Icons.Material.Filled.CalendarMonth;
            default:
                return Icons.Material.Filled.Settings;
        }
    }

    public static DataMapType GetDataMapType(this KeyValuePair<string, object> kv)
    {
        /*
        bool	System.Boolean
        byte	System.Byte
        sbyte	System.SByte
        char	System.Char
        decimal	System.Decimal
        double	System.Double
        float	System.Single
        int	System.Int32
        uint	System.UInt32
        nint	System.IntPtr
        nuint	System.UIntPtr
        long	System.Int64
        ulong	System.UInt64
        short	System.Int16
        ushort	System.UInt16
        */
        switch (kv.Value.GetType().FullName)
        {
            case "System.String":
                return DataMapType.String;
            case "System.Int32":
                return DataMapType.Integer;
            case "System.Int64":
                return DataMapType.Long;
            case "System.Boolean":
                return DataMapType.Bool;
            case "System.Single":
                return DataMapType.Float;
            case "System.Decimal":
                return DataMapType.Decimal;
            case "System.Double":
                return DataMapType.Double;
            case "System.Int16":
                return DataMapType.Short;
            case "System.Char":
                return DataMapType.Char;
        }

        return DataMapType.Object;
    }

    public static string GetDataMapTypeDescription(this KeyValuePair<string, object> kv)
    {
        var mapType = kv.GetDataMapType();
        if (mapType == DataMapType.Object)
        {
            return $"Object ({kv.Value.GetType().FullName})";
        }

        return mapType.ToString();
    }

    /// <summary>
    /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.  Examples: 3.1 seconds, 2 minutes, 4.23 hours, etc.
    /// </summary>
    /// <param name="span">The timespan.</param>
    /// <param name="significantDigits">Significant digits to use for output.</param>
    /// <returns></returns>
    public static string ToHumanTimeString(this TimeSpan span, int significantDigits = 3)
    {
        var format = "G" + significantDigits;
        return span.TotalMilliseconds < 1000
            ? span.TotalMilliseconds.ToString(format) + " ms"
            : (
                span.TotalSeconds < 60
                    ? span.TotalSeconds.ToString(format)
                        + (span.TotalSeconds == 1 ? " sec" : " secs")
                    : (
                        span.TotalMinutes < 60
                            ? span.TotalMinutes.ToString(format)
                                + (span.TotalMinutes == 1 ? " min" : " mins")
                            : (
                                span.TotalHours < 24
                                    ? span.TotalHours.ToString(format)
                                        + (span.TotalHours == 1 ? " hr" : " hrs")
                                    : span.TotalDays.ToString(format)
                                        + (span.TotalDays == 1 ? " day" : " days")
                            )
                    )
            );
    }

    public static bool EqualsTriggerKey(this ScheduleModel model, TriggerKey triggerKey)
    {
        return model.TriggerName == triggerKey.Name && model.TriggerGroup == triggerKey.Group;
    }

    public static bool Equals(this ScheduleModel model, JobKey? jobKey, TriggerKey? triggerKey)
    {
        if (jobKey != null && triggerKey != null)
            return model.JobName == jobKey.Name
                && model.JobGroup == jobKey.Group
                && model.TriggerName == triggerKey.Name
                && model.TriggerGroup == triggerKey.Group;

        if (jobKey != null && triggerKey == null)
            return model.JobName == jobKey.Name
                && model.JobGroup == jobKey.Group
                && model.TriggerName == null
                && model.TriggerGroup == null;

        // less possible
        if (jobKey == null && triggerKey != null)
            return model.TriggerName == triggerKey.Name
                && model.TriggerGroup == triggerKey.Group
                && model.JobName == null
                && model.JobGroup == BlazingQuartz.Constants.DEFAULT_GROUP;

        return model.JobName == null && model.TriggerName == null && model.TriggerGroup == null;
    }

    public static TriggerType GetTriggerType(this ITrigger trigger)
    {
        if (trigger is ICronTrigger)
            return TriggerType.Cron;
        if (trigger is ISimpleTrigger)
            return TriggerType.Simple;
        if (trigger is ICalendarIntervalTrigger)
            return TriggerType.Calendar;
        if (trigger is IDailyTimeIntervalTrigger)
            return TriggerType.Daily;

        return TriggerType.Unknown;
    }

    public static TimeOfDay ToTimeOfDay(this TimeSpan timeSpan)
    {
        return new TimeOfDay(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
    }

    public static Quartz.IntervalUnit ToQuartzIntervalUnit(this IntervalUnit value)
    {
        return Enum.Parse<Quartz.IntervalUnit>(value.ToString());
    }

    public static IntervalUnit ToBlazingQuartzIntervalUnit(this Quartz.IntervalUnit value)
    {
        return Enum.Parse<IntervalUnit>(value.ToString());
    }

    public static JobKey ToJobKey(this Key key)
    {
        return key.Group == null ? new JobKey(key.Name) : new JobKey(key.Name, key.Group);
    }

    public static TriggerKey ToTriggerKey(this Key key)
    {
        return key.Group == null ? new TriggerKey(key.Name) : new TriggerKey(key.Name, key.Group);
    }

    /// <summary>
    /// Return closest non null stack trace of exception.
    /// Loop until null InnerException to get stack trace.
    /// </summary>
    /// <param name="exception"></param>
    /// <returns>null if inner exceptions does not have stack trace</returns>
    public static string? NonNullStackTrace(this Exception exception)
    {
        Exception? currentException = exception;
        while (currentException.StackTrace == null)
        {
            currentException = currentException.InnerException;
            if (currentException == null)
                break;
        }
        return currentException?.StackTrace;
    }
}
