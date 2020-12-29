using System;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public class ServerChanBatchedSink : BatchedSink
    {
        private readonly string _scKey;

        public ServerChanBatchedSink(
            string scKey,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _scKey = scKey;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_scKey.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override IPushService PushService => new ServerChanApiClient(_scKey);

        protected override string RenderMessage(LogEvent logEvent)
        {
            var msg = base.RenderMessage(logEvent);
            msg += "\r\n";
            return msg;
        }

        public override void Dispose()
        {
            //todo
        }
    }
}
