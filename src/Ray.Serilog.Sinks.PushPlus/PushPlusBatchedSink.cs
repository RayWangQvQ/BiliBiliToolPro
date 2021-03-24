using System;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;
using Serilog.Formatting;

namespace Ray.Serilog.Sinks.PushPlus
{
    public class PushPlusBatchedSink : BatchedSink
    {
        private readonly string _token;
        private readonly string _topic;

        public PushPlusBatchedSink(
            string token,
            string topic,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            string outputTemplate,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            )
            : base(predicate, sendBatchesAsOneMessages, outputTemplate, formatProvider, minimumLogEventLevel)
        {
            _token = token;
            _topic = topic;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_token.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override IPushService PushService => new PushPlusApiClient(_token, _topic);

        public override void Dispose()
        {
            //todo
        }
    }
}
