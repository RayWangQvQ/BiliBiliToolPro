using System;
using Ray.Serilog.Sinks.Batched;
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
            string turboScKey,
            string containsTrigger = Constants.DefaultContainsTrigger,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            if (containsTrigger.IsNullOrEmpty()) containsTrigger = Constants.DefaultContainsTrigger;
            Predicate<LogEvent> predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);

            return loggerSinkConfiguration.Sink(new ServerChanBatchedSink(scKey, turboScKey, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
