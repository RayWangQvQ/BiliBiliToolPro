using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class WbiServiceTests
{
    [Fact]
    public void EncWbi_UnsortedParameters_ProducesKnownSignature()
    {
        var service = new WbiService(NullLogger<WbiService>.Instance, null!);
        var parameters = new Dictionary<string, string>
        {
            ["baz"] = "1919810",
            ["foo"] = "114",
            ["bar"] = "514",
        };

        var signature = service.EncWbi(
            parameters,
            "653657f524a547ac981ded72ea172057",
            "6e4909c702f846728e64f6007736a338",
            1684746387
        );

        Assert.Equal(1684746387, signature.wts);
        Assert.Equal("d3cbd2a2316089117134038bf4caf442", signature.w_rid);
        Assert.Equal("1919810", parameters["baz"]);
    }

    [Fact]
    public void EncWbi_CharactersFilteredFromValues_ProducesSameSignature()
    {
        var service = new WbiService(NullLogger<WbiService>.Instance, null!);
        var clean = new Dictionary<string, string> { ["foo"] = "hello world" };
        var decorated = new Dictionary<string, string> { ["foo"] = "he!ll(o)' wor*ld" };

        var expected = service.EncWbi(clean, new string('a', 32), new string('b', 32), 123);
        var actual = service.EncWbi(decorated, new string('a', 32), new string('b', 32), 123);

        Assert.Equal(expected.w_rid, actual.w_rid);
    }
}
