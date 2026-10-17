using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Infrastructure.Notifications;
using Xunit;

namespace Ray.BiliBiliTool.Infrastructure.UnitTests;

public class NotificationLoggingTests
{
    [Fact]
    public async Task SendSummaryAsync_MultilineBody_PreservesLinesWithoutNetwork()
    {
        var logger = new CaptureLogger();
        var adapter = new SerilogNotificationAdapter(logger);

        await adapter.SendSummaryAsync("Daily", [new SummaryLine("Tasks", "one\ntwo")], "daily");

        Assert.Equal(LogLevel.Information, logger.Level);
        Assert.Equal($"[NOTIFICATION] Daily{Environment.NewLine}• Tasks: one\ntwo", logger.Message);
    }

    private sealed class CaptureLogger : ILogger<SerilogNotificationAdapter>
    {
        public LogLevel? Level { get; private set; }
        public string? Message { get; private set; }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Level = logLevel;
            Message = formatter(state, exception);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose() { }
    }
}
