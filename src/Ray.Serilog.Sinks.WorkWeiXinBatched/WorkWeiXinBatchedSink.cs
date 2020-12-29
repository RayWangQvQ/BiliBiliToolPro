using System;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.WorkWeiXinBatched
{
    public class WorkWeiXinBatchedSink : BatchedSink
    {
        private readonly string _webHookUrl;

        public WorkWeiXinBatchedSink(
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

        protected override IPushService PushService => new WorkWeiXinApiClient(_webHookUrl);

        public override void Dispose()
        {
            //todo
        }
    }
}
