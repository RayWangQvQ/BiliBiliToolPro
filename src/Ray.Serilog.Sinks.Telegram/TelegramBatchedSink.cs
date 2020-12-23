using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.Telegram
{
    public class TelegramBatchedSink : ILogEventSink, IDisposable
    {
        private readonly string _botToken;
        private readonly string _chatId;
        private readonly LogEventLevel _minimumLogEventLevel;
        private readonly Predicate<LogEvent> _predicate;
        private readonly bool _sendBatchesAsOneMessages;
        private readonly IFormatProvider _formatProvider;

        private readonly BoundedConcurrentQueue<LogEvent> _queue = new BoundedConcurrentQueue<LogEvent>();

        public TelegramBatchedSink(
            string botToken,
            string chatId,
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel
            )
        {
            _predicate = predicate ?? (x => true);
            _minimumLogEventLevel = minimumLogEventLevel;
            _sendBatchesAsOneMessages = sendBatchesAsOneMessages;
            _formatProvider = formatProvider;
            _botToken = botToken;
            _chatId = chatId;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");

            try
            {
                if (logEvent.Level < _minimumLogEventLevel) return;
                _queue.TryEnqueue(logEvent);

                if (_predicate(logEvent))
                {
                    var waitingBatch = new Queue<LogEvent>();
                    while (_queue.TryDequeue(out LogEvent item))
                    {
                        waitingBatch.Enqueue(item);
                    }
                    EmitBatchAsync(waitingBatch).Wait();
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);
            }
        }

        protected async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (_sendBatchesAsOneMessages)
            {
                var sb = new StringBuilder();
                var count = 0;
                foreach (var logEvent in events)
                {
                    var message = this._formatProvider != null
                                      ? logEvent.RenderMessage(_formatProvider)
                                      : RenderMessage(logEvent);

                    if (count == events.Count())
                    {
                        sb.AppendLine(message);
                        sb.AppendLine(Environment.NewLine);
                    }
                    else
                    {
                        sb.AppendLine(message);
                    }

                    count++;
                }

                var messageToSend = sb.ToString();
                await SendMessage(this._botToken, this._chatId, messageToSend);
            }
            else
            {
                foreach (var logEvent in events)
                {
                    var message = this._formatProvider != null
                                      ? logEvent.RenderMessage(_formatProvider)
                                      : RenderMessage(logEvent);
                    await SendMessage(_botToken, _chatId, message);
                }
            }
        }

        private static string RenderMessage(LogEvent logEvent)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetEmoji(logEvent)} {logEvent.RenderMessage()}");

            if (logEvent.Exception == null)
            {
                return sb.ToString();
            }

            sb.AppendLine($"\n*{logEvent.Exception.Message}*\n");
            sb.AppendLine($"Message: `{logEvent.Exception.Message}`");
            sb.AppendLine($"Type: `{logEvent.Exception.GetType().Name}`\n");
            sb.AppendLine($"Stack Trace\n```{logEvent.Exception}```");

            return sb.ToString();
        }

        private static string GetEmoji(LogEvent log)
        {
            switch (log.Level)
            {
                case LogEventLevel.Verbose:
                    return "⚡";
                case LogEventLevel.Debug:
                    return "👉";
                case LogEventLevel.Information:
                    return "ℹ";
                case LogEventLevel.Warning:
                    return "⚠";
                case LogEventLevel.Error:
                    return "❗";
                case LogEventLevel.Fatal:
                    return "‼";
                default:
                    return string.Empty;
            }
        }

        private static async Task SendMessage(string token, string chatId, string message)
        {
            SelfLog.WriteLine($"Trying to send message to chatId '{chatId}': '{message}'.");
            var client = new TelegramApiClient(token, 5);
            var result = await client.PostMessageAsync(message, chatId);

            if (result != null)
            {
                SelfLog.WriteLine($"Message sent to chatId '{chatId}': '{result.StatusCode}'.");
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
