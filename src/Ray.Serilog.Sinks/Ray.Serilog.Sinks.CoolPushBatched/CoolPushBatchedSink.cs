using System;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.CoolPushBatched
{
    public class CoolPushBatchedSink : BatchedSink
    {
        private readonly string _sKey;

        public CoolPushBatchedSink(
            string sKey,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _sKey = sKey;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_sKey.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override PushService PushService => new CoolPushApiClient(_sKey);

        public override void Dispose()
        {
            //todo
        }
    }
}
