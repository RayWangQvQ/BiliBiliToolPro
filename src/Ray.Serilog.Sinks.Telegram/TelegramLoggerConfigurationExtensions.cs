using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Serilog.Sinks.Telegram
{
    public static class TelegramLoggerConfigurationExtensions
    {
        public static LoggerConfiguration TelegramBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string botToken,
            string chatId,
            Predicate<LogEvent> predicate = null,
            bool sendBatchesAsOneMessages = true,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
            )
        {
            if (predicate == null) predicate = x => true;
            return loggerSinkConfiguration.Sink(new TelegramBatchedSink(botToken, chatId, predicate, sendBatchesAsOneMessages, formatProvider, restrictedToMinimumLevel), restrictedToMinimumLevel);
        }
    }
}
