using System;
using Ray.Serilog.Sinks.Batched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.WorkWeiXinAppBatched
{
    public static class WorkWeiXinAppConfigurationExtensions
    {
        public static LoggerConfiguration WorkWeiXinAppBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string corpId,
            string agentId,
            string secret,
            string toUser,
            string toParty,
            string toTag,
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
                new WorkWeiXinAppBatchedSink(
                    corpId,
                    agentId,
                    secret,
                    toUser,
                    toParty,
                    toTag,
                    predicate,
                    sendBatchesAsOneMessages,
                    outputTemplate,
                    formatProvider,
                    restrictedToMinimumLevel),
                restrictedToMinimumLevel);
        }
    }
}
