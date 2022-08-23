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

        public TelegramBatchedSink(
            string botToken,
            string chatId,
            string proxy,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            ) : base(predicate, sendBatchesAsOneMessages, formatProvider, minimumLogEventLevel)
        {
            _botToken = botToken;
            _chatId = chatId;
            _proxy = proxy;
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_botToken.IsNullOrEmpty() | _chatId.IsNullOrEmpty()) return;
            base.Emit(logEvent);
        }

        protected override PushService PushService => new TelegramApiClient(_botToken, _chatId, _proxy, 5);

        public override void Dispose()
        {
            //todo
        }
    }
}
