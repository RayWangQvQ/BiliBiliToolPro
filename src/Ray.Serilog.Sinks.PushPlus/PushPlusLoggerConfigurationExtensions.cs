using System;
using Ray.Serilog.Sinks.Batched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Ray.Serilog.Sinks.PushPlus
{
    public static class PushPlusLoggerConfigurationExtensions
    {
        public static LoggerConfiguration PushPlusBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string token,
            string topic = "",
            string containsTrigger = Constants.DefaultContainsTrigger,
            bool sendBatchesAsOneMessages = true,
            string outputTemplate = Constants.DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            if (loggerSinkConfiguration == null)
                throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (outputTemplate == null)
                throw new ArgumentNullException(nameof(outputTemplate));

            if (containsTrigger.IsNullOrEmpty()) containsTrigger = Constants.DefaultContainsTrigger;
            Predicate<LogEvent> predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);

            return loggerSinkConfiguration.Sink(
                new PushPlusBatchedSink(
                    token,
                    topic,
                    predicate,
                    sendBatchesAsOneMessages,
                    outputTemplate,
                    formatProvider,
                    restrictedToMinimumLevel),
                restrictedToMinimumLevel);
        }
    }
}
