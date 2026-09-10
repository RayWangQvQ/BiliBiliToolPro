using Ray.BiliBiliTool.CharacterizationTests.Support;

namespace Ray.BiliBiliTool.CharacterizationTests;

public class CharacterizationHarnessSmokeTests
{
    [Fact]
    public void Test_log_collector_starts_empty()
    {
        using var collector = new TestLogCollector();

        collector.Entries.Should().BeEmpty();
    }
}
