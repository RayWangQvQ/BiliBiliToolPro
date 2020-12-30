using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.OtherApiBatched
{
    public static class OtherApiLoggerConfigurationExtensions
    {
        public static LoggerConfiguration OtherApiBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string api,
            string bodyJsonTemplate,
            string placeholder,
            string containsTrigger = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            Predicate<LogEvent> predicate = null;
            if (containsTrigger.IsNotNullOrEmpty()) predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);
            return loggerSinkConfiguration.OtherApiBatched(api, bodyJsonTemplate, placeholder, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration OtherApiBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string api,
            string bodyJsonTemplate,
            string placeholder,
            Predicate<LogEvent> triggerPredicate = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            if (triggerPredicate == null) triggerPredicate = x => true;
            return loggerSinkConfiguration.Sink(new OtherApiBatchedSink(api, bodyJsonTemplate, placeholder, triggerPredicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
