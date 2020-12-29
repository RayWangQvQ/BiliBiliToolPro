using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.CoolPushBatched
{
    public static class CoolPushLoggerConfigurationExtensions
    {
        public static LoggerConfiguration CoolPushBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string sKey,
            string containsTrigger = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            Predicate<LogEvent> predicate = null;
            if (containsTrigger.IsNotNullOrEmpty()) predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);
            return loggerSinkConfiguration.CoolPushBatched(sKey, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration CoolPushBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string sKey,
            Predicate<LogEvent> triggerPredicate = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            if (triggerPredicate == null) triggerPredicate = x => true;
            return loggerSinkConfiguration.Sink(new CoolPushBatchedSink(sKey, triggerPredicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
