using System.Text.Json;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveLotteryPendantTests
{
    private static JsonElement? Pendant(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData(null)]
    [InlineData("[]")]
    [InlineData("123")]
    [InlineData("{\"1\":{\"pendent_id\":426}}")]
    [InlineData("{\"2\":null}")]
    [InlineData("{\"2\":{\"content\":\"天选时刻\"}}")]
    [InlineData("{\"2\":{\"pendent_id\":397}}")]
    [InlineData("{\"2\":{\"pendent_id\":\"504\"}}")]
    public void NonTianXuanPendant_ReturnsFalse(string? json)
    {
        JsonElement? el = json is null ? null : Pendant(json);

        Assert.False(LiveDomainService.IsTianXuanPendant(el));
    }

    [Theory]
    [InlineData("{\"2\":{\"pendent_id\":504}}")]
    [InlineData("{\"2\":{\"pendent_id\":1432}}")]
    public void TianXuanPendant_ReturnsTrue(string json)
    {
        Assert.True(LiveDomainService.IsTianXuanPendant(Pendant(json)));
    }
}
