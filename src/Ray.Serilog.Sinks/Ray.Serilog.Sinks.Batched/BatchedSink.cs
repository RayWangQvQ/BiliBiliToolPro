using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Ray.Serilog.Sinks.Batched
{
    public abstract class BatchedSink : ILogEventSink, IDisposable
    {
        private readonly LogEventLevel _minimumLogEventLevel;
        private readonly Predicate<LogEvent> _predicate;
        private readonly bool _sendBatchesAsOneMessages;
        private readonly ITextFormatter _formatter;

        private readonly BoundedConcurrentQueue<LogEvent> _queue = new BoundedConcurrentQueue<LogEvent>();

        public BatchedSink(
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            IFormatProvider formatProvider,
            LogEventLevel minimumLogEventLevel)
            : this(predicate, sendBatchesAsOneMessages, null, formatProvider, minimumLogEventLevel)
        {
        }

        public BatchedSink(
            Predicate<LogEvent> predicate,
            bool sendBatchesAsOneMessages,
            string outputTemplate = "{Message:lj}{NewLine}{Exception}",
            IFormatProvider formatProvider = null,
            LogEventLevel minimumLogEventLevel = LogEventLevel.Verbose)
        {
            _predicate = predicate ?? (x => true);
            _minimumLogEventLevel = minimumLogEventLevel;
            _sendBatchesAsOneMessages = sendBatchesAsOneMessages;

            outputTemplate = string.IsNullOrWhiteSpace(outputTemplate)
                ? Constants.DefaultOutputTemplate
                : outputTemplate;
            _formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
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
                foreach (var logEvent in events)
                {
                    string message = RenderMessage(logEvent);
                    sb.Append(message);
                }
                sb.AppendLine(Environment.NewLine);

                var messageToSend = sb.ToString();
                PushMessage(messageToSend);
            }
            else
            {
                foreach (var logEvent in events)
                {
                    var message = RenderMessage(logEvent);
                    PushMessage(message);
                }
            }
        }

        protected abstract PushService PushService { get; }

        protected virtual void PushMessage(string message, string title = "Ray.BiliBiliTool任务推送")
        {
            //SelfLog.WriteLine($"Trying to send message: '{message}'.");
            var result = PushService.PushMessage(message, title);
            if (result != null)
            {
                SelfLog.WriteLine($"Response status: {result.StatusCode}.");
                try
                {
                    var content = result.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult()
                        .Replace("{", "{{")
                        .Replace("}", "}}");
                    SelfLog.WriteLine($"Response content: {content}.{Environment.NewLine}");
                }
                catch (Exception e)
                {
                    SelfLog.WriteLine(e.Message + Environment.NewLine);
                }
            }
        }

        protected virtual string RenderMessage(LogEvent logEvent)
        {
            string msg = "";
            using (StringWriter stringWriter = new StringWriter())
            {
                this._formatter.Format(logEvent, (TextWriter)stringWriter);
                msg = stringWriter.ToString();
            }

            msg = $"{GetEmoji(logEvent)} {msg}";

            if (msg.Contains("经验+") && msg.Contains("√"))
                msg = msg.Replace('√', '✔');

            return msg;

            /*
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
            */
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
