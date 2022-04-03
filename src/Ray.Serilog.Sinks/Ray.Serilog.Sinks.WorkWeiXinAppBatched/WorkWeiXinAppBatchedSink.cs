using System;
using Ray.Serilog.Sinks.Batched;
using Serilog.Events;

namespace Ray.Serilog.Sinks.WorkWeiXinAppBatched
{
    public class WorkWeiXinAppBatchedSink : BatchedSink
    {
        private readonly string _corpId;
        private readonly string _agentId;
        private readonly string _secret;

        private readonly string _toUser;
        private readonly string _toParty;
        private readonly string _toTag;

        public WorkWeiXinAppBatchedSink(
            string corpId,
            string agentId,
            string secret,
            string toUser,
            string toParty,
            string toTag,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            string outputTemplate,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            )
            : base(predicate, sendBatchesAsOneMessages, outputTemplate, formatProvider, minimumLogEventLevel)
        {
            _corpId = corpId;
            _agentId = agentId;
            _secret = secret;
            _toUser = toUser;
            _toParty = toParty;
            _toTag = toTag;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_corpId.IsNullOrEmpty()||_secret.IsNullOrEmpty()||_agentId.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override PushService PushService => new WorkWeiXinAppApiClient(
            _corpId,
            _agentId,
            _secret,
            _toUser,
            _toParty,
            _toTag);

        public override void Dispose()
        {
            //todo
        }
    }
}
