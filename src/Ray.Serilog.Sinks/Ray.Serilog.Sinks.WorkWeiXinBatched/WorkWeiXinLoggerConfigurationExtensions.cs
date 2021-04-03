using System;
using Ray.Serilog.Sinks.Batched;
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
            string containsTrigger = Constants.DefaultContainsTrigger,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            if (containsTrigger.IsNullOrEmpty()) containsTrigger = Constants.DefaultContainsTrigger;
            Predicate<LogEvent> predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);

            return loggerSinkConfiguration.Sink(new WorkWeiXinBatchedSink(webHookUrl, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
