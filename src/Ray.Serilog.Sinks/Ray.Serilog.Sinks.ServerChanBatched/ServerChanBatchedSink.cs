using System;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public class ServerChanBatchedSink : BatchedSink
    {
        private readonly string _scKey;
        private readonly string _turboScKey;

        public ServerChanBatchedSink(
            string scKey,
            string turboScKey,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _scKey = scKey;
            _turboScKey = turboScKey;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_scKey.IsNullOrEmpty() && _turboScKey.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override PushService PushService
        {
            get
            {
                if (_turboScKey.IsNotNullOrEmpty()) return new ServerChanTurboApiClient(_turboScKey);
                return new ServerChanApiClient(_scKey);
            }
        }

        public override void Dispose()
        {
            //todo
        }
    }
}
