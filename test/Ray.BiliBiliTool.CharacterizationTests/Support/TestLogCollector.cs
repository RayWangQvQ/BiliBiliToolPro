using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.CharacterizationTests.Support;

public sealed class TestLogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<TestLogEntry> _entries = new();

    public IReadOnlyCollection<TestLogEntry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CollectorLogger(categoryName, _entries);

    public void Dispose() { }

    private sealed class CollectorLogger(string categoryName, ConcurrentQueue<TestLogEntry> entries)
        : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            entries.Enqueue(
                new TestLogEntry(
                    categoryName,
                    logLevel,
                    eventId,
                    formatter(state, exception),
                    exception
                )
            );
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}

public sealed record TestLogEntry(
    string Category,
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception
);
