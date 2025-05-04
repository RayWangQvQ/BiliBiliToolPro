using System;
using System.Text;
using Quartz;

namespace BlazingQuartz.Core.Models
{
    public class TriggerDetailModel
    {
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = Constants.DEFAULT_GROUP;
        public TriggerType TriggerType { get; set; }
        public string? Description { get; set; }
        public TimeSpan? StartTimeSpan { get; set; }
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Combined <see cref="StartDate"/> <see cref="StartTimeSpan"/> <see cref="StartTimezone"/>
        /// </summary>
        public DateTimeOffset? StartDateTimeUtc
        {
            get
            {
                if (StartDate.HasValue)
                {
                    DateTimeOffset startTime;

                    if (StartTimeSpan.HasValue)
                    {
                        var dt = StartDate.Value.Add(StartTimeSpan.Value);
                        startTime = new DateTimeOffset(dt, StartTimezone.BaseUtcOffset);
                    }
                    else
                    {
                        startTime = new DateTimeOffset(
                            StartDate.Value,
                            StartTimezone.BaseUtcOffset
                        );
                    }

                    return startTime;
                }
                return null;
            }
        }
        public TimeSpan? EndTimeSpan { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTimeOffset? EndDateTimeUtc
        {
            get
            {
                if (EndDate.HasValue)
                {
                    DateTimeOffset endTime;

                    if (EndTimeSpan.HasValue)
                    {
                        var dt = EndDate.Value.Add(EndTimeSpan.Value);
                        endTime = new DateTimeOffset(dt, StartTimezone.BaseUtcOffset);
                    }
                    else
                    {
                        endTime = new DateTimeOffset(EndDate.Value, StartTimezone.BaseUtcOffset);
                    }

                    return endTime;
                }
                else
                {
                    return null;
                }
            }
        }

        public string? ModifiedByCalendar { get; set; }

        /// <summary>
        /// Timezone of start time
        /// </summary>
        public TimeZoneInfo StartTimezone { get; set; } = TimeZoneInfo.Utc;
        public int Priority { get; set; } = 5;
        public string? CronExpression { get; set; }
        public bool RepeatForever { get; set; }
        public int RepeatCount { get; set; }

        public bool[] DailyDayOfWeek { get; set; } = new bool[7];
        public TimeSpan? StartDailyTime { get; set; }
        public TimeSpan? EndDailyTime { get; set; }

        /// <summary>
        /// The timezone in which to base the scheduled. Used in Cron schedule, Calendar schedule and Daily schedule.
        /// </summary>
        public TimeZoneInfo InTimeZone { get; set; } = TimeZoneInfo.Local;

        public int TriggerInterval { get; set; } = 1;
        public IntervalUnit? TriggerIntervalUnit { get; set; } = IntervalUnit.Minute;
        public MisfireAction MisfireAction { get; set; } = MisfireAction.SmartPolicy;

        public IDictionary<string, object> TriggerDataMap { get; set; } =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<DayOfWeek> GetDailyOnDaysOfWeek()
        {
            var dayOfWeekCount = 7;
            var list = new List<DayOfWeek>(dayOfWeekCount);
            for (int i = 0; i < dayOfWeekCount; i++)
            {
                if (DailyDayOfWeek[i])
                {
                    list.Add((DayOfWeek)Enum.ToObject(typeof(DayOfWeek), i));
                }
            }

            return list;
        }

        public string ToSummaryString()
        {
            var bldr = new StringBuilder();
            switch (TriggerType)
            {
                case TriggerType.Cron:
                    bldr.AppendLine(
                        CronExpressionDescriptor.ExpressionDescriptor.GetDescription(CronExpression)
                    );
                    break;
                case TriggerType.Daily:
                    bldr.AppendJoin(
                        ", ",
                        DailyDayOfWeek.Where(f => f).Select((f, i) => (DayOfWeek)i)
                    );
                    if (EndDailyTime.HasValue)
                    {
                        bldr.AppendLine(
                            $" from {StartDailyTime.ToString()} to {EndDailyTime.ToString()} {InTimeZone.DisplayName}"
                        );
                    }
                    else
                    {
                        bldr.AppendLine(
                            $" at {StartDailyTime.ToString()} {InTimeZone.DisplayName}"
                        );
                    }
                    break;
                case TriggerType.Simple:
                    bldr.Append($"Every {TriggerInterval} {TriggerIntervalUnit?.ToString()}.");
                    if (RepeatForever)
                    {
                        bldr.AppendLine(" Repeat forever.");
                    }
                    else if (RepeatCount > 0)
                    {
                        bldr.AppendLine($" Repeat {RepeatCount} time(s).");
                    }
                    break;
                case TriggerType.Calendar:
                    bldr.Append(
                        $"{ModifiedByCalendar} calendar. Start at {StartDate} {StartTimeSpan} {InTimeZone}. Repeat every {TriggerInterval} {TriggerIntervalUnit?.ToString()}"
                    );
                    break;
            }

            return bldr.ToString();
        }
    }
}
