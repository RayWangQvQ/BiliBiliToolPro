using System;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.DingTalkBatched
{
    public class DingTalkBatchedSink : BatchedSink
    {
        private readonly string _webHookUrl;

        public DingTalkBatchedSink(
            string webHookUrl,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _webHookUrl = webHookUrl;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_webHookUrl.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override IPushService PushService => new DingTalkApiClient(_webHookUrl);

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
