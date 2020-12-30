using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.OtherApiBatched
{
    public class OtherApiBatchedSink : BatchedSink
    {
        private readonly string _api;
        private readonly string _jsonTemplate;
        private readonly string _placeholder;

        public OtherApiBatchedSink(
            string api,
            string jsonTemplate,
            string placeholder,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _api = api;
            _jsonTemplate = jsonTemplate;
            _placeholder = placeholder;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_api.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override IPushService PushService => new OtherApiClient(_api, _jsonTemplate, _placeholder);

        public override void Dispose()
        {
            //todo
        }
    }
}
