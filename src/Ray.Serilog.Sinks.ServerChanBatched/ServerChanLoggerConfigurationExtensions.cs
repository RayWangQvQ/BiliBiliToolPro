using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public static class ServerChanLoggerConfigurationExtensions
    {
        public static LoggerConfiguration ServerChanBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string scKey,
            string containsTrigger = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            Predicate<LogEvent> predicate = null;
            if (containsTrigger.IsNotNullOrEmpty()) predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);
            return loggerSinkConfiguration.ServerChanBatched(scKey, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration ServerChanBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string scKey,
            Predicate<LogEvent> triggerPredicate = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            if (triggerPredicate == null) triggerPredicate = x => true;
            return loggerSinkConfiguration.Sink(new ServerChanBatchedSink(scKey, triggerPredicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
