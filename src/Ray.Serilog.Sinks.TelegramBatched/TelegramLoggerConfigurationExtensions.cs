using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.TelegramBatched
{
    public static class TelegramLoggerConfigurationExtensions
    {
        public static LoggerConfiguration TelegramBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string botToken,
            string chatId,
            string containsTrigger = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        {
            Predicate<LogEvent> predicate = null;
            if (containsTrigger.IsNotNullOrEmpty()) predicate = x => x.MessageTemplate.Text.Contains(containsTrigger);
            return loggerSinkConfiguration.TelegramBatched(botToken, chatId, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration TelegramBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string botToken,
            string chatId,
            Predicate<LogEvent> triggerPredicate = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            if (triggerPredicate == null) triggerPredicate = x => true;
            return loggerSinkConfiguration.Sink(new TelegramBatchedSink(botToken, chatId, triggerPredicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
