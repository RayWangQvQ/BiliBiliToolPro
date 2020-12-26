using System;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.WorkWeiXinBatched
{
    public static class WorkWeiXinLoggerConfigurationExtensions
    {
        public static LoggerConfiguration WorkWeiXinBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string webHookUrl,
            string containsTrigger = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            Predicate<LogEvent> predicate = null;
            if (containsTrigger.IsNotNullOrEmpty()) predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);
            return loggerSinkConfiguration.WorkWeiXinBatched(webHookUrl, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration WorkWeiXinBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string webHookUrl,
            Predicate<LogEvent> triggerPredicate = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            if (triggerPredicate == null) triggerPredicate = x => true;
            return loggerSinkConfiguration.Sink(new WorkWeiXinBatchedSink(webHookUrl, triggerPredicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
