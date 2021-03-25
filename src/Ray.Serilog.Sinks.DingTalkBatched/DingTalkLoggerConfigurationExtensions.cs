using System;
using Ray.Serilog.Sinks.Batched;
using Ray.Serilog.Sinks.DingTalkBatched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.DingTalkBatched
{
    public static class DingTalkLoggerConfigurationExtensions
    {
        public static LoggerConfiguration DingTalkBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string webHookUrl,
            string containsTrigger = Constants.DefaultContainsTrigger,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            if (containsTrigger.IsNullOrEmpty()) containsTrigger = Constants.DefaultContainsTrigger;
            Predicate<LogEvent> predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);

            return loggerSinkConfiguration.Sink(new DingTalkBatchedSink(webHookUrl, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
