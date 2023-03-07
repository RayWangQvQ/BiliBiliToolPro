using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Ray.Serilog.Sinks.TelegramBatched
{
    public class TelegramBatchedSink : BatchedSink
    {
        private readonly string _botToken;
        private readonly string _chatId;
        private readonly string _proxy;
        private readonly string _apiHost;

        public TelegramBatchedSink(
            string botToken,
            string chatId,
            string proxy,
            string apiHost,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _botToken = botToken;
            _chatId = chatId;
            _proxy = proxy;
            _apiHost = apiHost;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_botToken.IsNullOrEmpty() | _chatId.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override PushService PushService => new TelegramApiClient(_botToken, _chatId, _proxy, _apiHost, 5);

        public override void Dispose()
        {
            //todo
        }
    }
}
