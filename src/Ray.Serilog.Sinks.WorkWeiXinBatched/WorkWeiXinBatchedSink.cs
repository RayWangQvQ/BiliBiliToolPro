using System;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.WorkWeiXinBatched
{
    public class WorkWeiXinBatchedSink : BatchedSink
    {
        private readonly string _key;

        public WorkWeiXinBatchedSink(
            string key,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _key = key;
        }

        protected override IPushService PushService => new WorkWeiXinApiClient(_key);

        public override void Dispose()
        {
            //todo
        }
    }
}
