using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Ray.Serilog.Sinks.Batched
{
    public abstract class BatchedSink : ILogEventSink, IDisposable
    {
        private readonly LogEventLevel _minimumLogEventLevel;
        private readonly Predicate<LogEvent> _predicate;
        private readonly bool _sendBatchesAsOneMessages;
        private readonly IFormatProvider _formatProvider;

        private readonly BoundedConcurrentQueue<LogEvent> _queue = new BoundedConcurrentQueue<LogEvent>();

        public BatchedSink(
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
        }

        public virtual void Emit(LogEvent logEvent)
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
                    EmitBatch(waitingBatch);
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex.Message);
            }
        }

        protected virtual void EmitBatch(IEnumerable<LogEvent> events)
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
                PushMessage(messageToSend);
            }
            else
            {
                foreach (var logEvent in events)
                {
                    var message = this._formatProvider != null
                                      ? logEvent.RenderMessage(_formatProvider)
                                      : RenderMessage(logEvent);
                    PushMessage(message);
                }
            }
        }

        protected abstract IPushService PushService { get; }

        protected virtual void PushMessage(string message)
        {
            //SelfLog.WriteLine($"Trying to send message: '{message}'.");
            var result = PushService.PushMessage(message);
            if (result != null)
            {
                SelfLog.WriteLine($"Response status: {result.StatusCode}.");
                try
                {
                    var content = result.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult()
                        .Replace("{", "{{")
                        .Replace("}", "}}");
                    SelfLog.WriteLine($"Response content: {content}.\r\n");
                }
                catch (Exception e)
                {
                    SelfLog.WriteLine(e.Message);
                }
            }
        }

        protected virtual string RenderMessage(LogEvent logEvent)
        {
            var msg = $"{GetEmoji(logEvent)} {logEvent.RenderMessage()}";

            if (msg.Contains("经验+") && msg.Contains("√"))
                msg = msg.Replace('√', '✔');

            if (logEvent.Exception == null)
            {
                return msg;
            }

            var sb = new StringBuilder();
            sb.AppendLine(msg);
            sb.AppendLine($"\n*{logEvent.Exception.Message}*\n");
            sb.AppendLine($"Message: `{logEvent.Exception.Message}`");
            sb.AppendLine($"Type: `{logEvent.Exception.GetType().Name}`\n");
            sb.AppendLine($"Stack Trace\n```{logEvent.Exception}```");

            return sb.ToString();
        }

        protected virtual string GetEmoji(LogEvent log)
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

        public abstract void Dispose();
    }
}
