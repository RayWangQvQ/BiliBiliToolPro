using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Infrastructure.UnitTests;

public class CookieStrFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(";; ")]
    [InlineData("not-a-cookie; =value")]
    public void CreateNew_EmptyOrMalformedInput_ReturnsEmptyCookie(string? value)
    {
        var cookie = CookieStrFactory<CookieInfo>.CreateNew(value!);
        cookie.CookieItemDictionary.Should().BeEmpty();
    }

    [Fact]
    public void Count_AllSlotsDeleted_ReturnsZero()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "",
                    ["BiliBiliCookies:1"] = " ",
                }
            )
            .Build();

        new CookieStrFactory<CookieInfo>(configuration).Count.Should().Be(0);
    }

    [Fact]
    public void CreateNew_EmptyOrMalformedSegments_PreservesValidValues()
    {
        var cookie = CookieStrFactory<CookieInfo>.CreateNew(
            " ; DedeUserID=123; invalid; =bad; SESSDATA=a=b=c;; DedeUserID=456; "
        );

        cookie.CookieItemDictionary.Should().HaveCount(2);
        cookie.CookieItemDictionary["DedeUserID"].Should().Be("123");
        cookie.CookieItemDictionary["SESSDATA"].Should().Be("a=b=c");
    }

    [Fact]
    public void GetCookie_DeletedOrMalformedSlots_KeepsValidAccountsContiguous()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "",
                    ["BiliBiliCookies:1"] = " ",
                    ["BiliBiliCookies:2"] = "DedeUserID=123; SESSDATA=one;",
                    ["BiliBiliCookies:3"] = "invalid",
                    ["BiliBiliCookies:4"] = "DedeUserID=456; SESSDATA=two",
                }
            )
            .Build();
        var factory = new CookieStrFactory<CookieInfo>(configuration);

        factory.Count.Should().Be(2);
        factory.GetCookie(0).CookieItemDictionary["DedeUserID"].Should().Be("123");
        factory.GetCookie(1).CookieItemDictionary["DedeUserID"].Should().Be("456");
    }
}
