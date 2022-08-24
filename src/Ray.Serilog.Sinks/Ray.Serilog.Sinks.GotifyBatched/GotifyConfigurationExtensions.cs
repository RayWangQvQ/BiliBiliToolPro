using Ray.Serilog.Sinks.Batched;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Ray.Serilog.Sinks.MicrosoftTeamsBatched
{
    public static class GotifyConfigurationExtensions
    {
        public static LoggerConfiguration GotifyBatched(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string host,
            string token,
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
                new GotifyBatchedSink(
                    host,
                    token,
                    predicate,
                    sendBatchesAsOneMessages,
                    outputTemplate,
                    formatProvider,
                    restrictedToMinimumLevel),
                restrictedToMinimumLevel);
        }
    }
}
