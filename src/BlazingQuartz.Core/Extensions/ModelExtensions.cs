using System;
using BlazingQuartz.Core.Models;
using Quartz;

namespace BlazingQuartz.Core
{
    public static class ModelExtensions
    {
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
                    && model.JobGroup == Constants.DEFAULT_GROUP;

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
            return key.Group == null
                ? new TriggerKey(key.Name)
                : new TriggerKey(key.Name, key.Group);
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
}
