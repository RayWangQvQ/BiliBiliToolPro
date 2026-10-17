using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.CharacterizationTests.Support;

namespace Ray.BiliBiliTool.CharacterizationTests;

public class TestLogCollectorTests
{
    [Fact]
    public void LogError_LoggingMessage_CapturesCategorySeverityAndException()
    {
        using var collector = new TestLogCollector();
        var logger = collector.CreateLogger("DailyTask");
        var error = new InvalidOperationException("failed");

        collector.Entries.Should().BeEmpty();
        logger.LogError(error, "Account {AccountId} failed", 42);

        var entry = collector.Entries.Should().ContainSingle().Which;
        entry.Category.Should().Be("DailyTask");
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().Be("Account 42 failed");
        entry.Exception.Should().BeSameAs(error);
    }
}
