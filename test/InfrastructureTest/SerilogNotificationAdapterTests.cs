using FluentAssertions;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Contracts.Notifications;
using Ray.BiliBiliTool.Infrastructure.Notifications;

namespace InfrastructureTest;

public class SerilogNotificationAdapterTests
{
    [Fact]
    public async Task SendAsync_logs_notification_at_correct_level()
    {
        var logger = new TestLogger<SerilogNotificationAdapter>();
        var adapter = new SerilogNotificationAdapter(logger);
        var message = new NotificationMessage("Test Title", "Test Body", NotificationLevel.Warning);

        await adapter.SendAsync(message, "group-1");

        logger.LastLevel.Should().Be(LogLevel.Warning);
        logger.LastMessage.Should().Contain("[NOTIFICATION] Test Title: Test Body");
    }

    [Fact]
    public async Task SendAsync_maps_info_level_to_information()
    {
        var logger = new TestLogger<SerilogNotificationAdapter>();
        var adapter = new SerilogNotificationAdapter(logger);
        var message = new NotificationMessage("Title", "Body", NotificationLevel.Info);

        await adapter.SendAsync(message, "g1");

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task SendAsync_maps_error_level_to_error()
    {
        var logger = new TestLogger<SerilogNotificationAdapter>();
        var adapter = new SerilogNotificationAdapter(logger);
        var message = new NotificationMessage("Title", "Body", NotificationLevel.Error);

        await adapter.SendAsync(message, "g1");

        logger.LastLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task SendSummaryAsync_formats_title_and_lines()
    {
        var logger = new TestLogger<SerilogNotificationAdapter>();
        var adapter = new SerilogNotificationAdapter(logger);
        var lines = new[] { new SummaryLine("投币", "5 个", "✅"), new SummaryLine("签到", "完成") };

        await adapter.SendSummaryAsync("Daily", lines, "g1");

        logger.LastLevel.Should().Be(LogLevel.Information);
        logger.LastMessage.Should().Contain("[NOTIFICATION] Daily");
        logger.LastMessage.Should().Contain("✅ 投币: 5 个");
        logger.LastMessage.Should().Contain("• 签到: 完成");
    }

    [Fact]
    public async Task SendSummaryAsync_defaults_icon_to_bullet_when_null()
    {
        var logger = new TestLogger<SerilogNotificationAdapter>();
        var adapter = new SerilogNotificationAdapter(logger);
        var lines = new[] { new SummaryLine("Key", "Value") };

        await adapter.SendSummaryAsync("Title", lines, "g1");

        logger.LastMessage.Should().Contain("• Key: Value");
    }

    [Fact]
    public void SendAsync_returns_completed_task()
    {
        var logger = new TestLogger<SerilogNotificationAdapter>();
        var adapter = new SerilogNotificationAdapter(logger);
        var message = new NotificationMessage("T", "B", NotificationLevel.Info);

        var task = adapter.SendAsync(message, "g1");

        task.IsCompleted.Should().BeTrue();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public LogLevel? LastLevel { get; private set; }

        public string? LastMessage { get; private set; }

        public Exception? LastException { get; private set; }

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
            LastLevel = logLevel;
            LastMessage = formatter(state, exception);
            LastException = exception;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose() { }
    }
}
