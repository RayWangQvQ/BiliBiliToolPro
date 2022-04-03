using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.MicrosoftTeamsBatched
{
    public class MicrosoftTeamsBatchedSink : BatchedSink
    {
        private readonly string _webhook;

        public MicrosoftTeamsBatchedSink(
            string webhook,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            string outputTemplate,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            )
            : base(predicate, sendBatchesAsOneMessages, outputTemplate, formatProvider, minimumLogEventLevel)
        {
            _webhook = webhook;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_webhook.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override PushService PushService => new MicrosoftTeamsApiClient(
            webhook: _webhook);

        public override void Dispose()
        {
            //todo
        }
    }
}
