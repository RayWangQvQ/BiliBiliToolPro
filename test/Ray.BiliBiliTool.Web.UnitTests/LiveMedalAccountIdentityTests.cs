using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public sealed class LiveMedalAccountIdentityTests
{
    private const string First = "DedeUserID=1;bili_jct=synthetic;SESSDATA=synthetic-a";
    private const string Second = "DedeUserID=2;bili_jct=synthetic;SESSDATA=synthetic-b";

    private static LiveMedalDashboardService Reader(IConfiguration config, IMemoryCache cache) =>
        new(
            new CookieStrFactory<BiliCookie>(config),
            null!,
            cache,
            NullLogger<LiveMedalDashboardService>.Instance
        );

    [Fact]
    public void AccountIdentityFollowsAccountWhenCookieOrderChanges()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = First,
                    ["BiliBiliCookies:1"] = Second,
                }
            )
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = Reader(config, cache);
        var before = reader.GetAccounts();
        config["BiliBiliCookies:0"] = Second;
        config["BiliBiliCookies:1"] = First;
        var after = reader.GetAccounts();
        Assert.NotEqual(before[0].Key, before[1].Key);
        Assert.Equal(before[0].Key, after[1].Key);
        Assert.Equal(before[1].Key, after[0].Key);
        Assert.Equal(1, after[1].Index);
    }

    [Fact]
    public void ReloginChangesIdentityButDeviceCookieEnrichmentDoesNot()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["BiliBiliCookies:0"] = First }
            )
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = Reader(config, cache);
        var original = Assert.Single(reader.GetAccounts());
        config["BiliBiliCookies:0"] = First + ";buvid3=synthetic-device";
        Assert.Equal(original.Key, Assert.Single(reader.GetAccounts()).Key);
        config["BiliBiliCookies:0"] = First.Replace("synthetic-a", "synthetic-new-session");
        Assert.NotEqual(original.Key, Assert.Single(reader.GetAccounts()).Key);
    }

    [Fact]
    public void AccountKeyAndCookieValuesAreExcludedFromSerializedAccount()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["BiliBiliCookies:0"] = First }
            )
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var account = Assert.Single(Reader(config, cache).GetAccounts());
        Assert.False(string.IsNullOrWhiteSpace(account.Key));
        var json = JsonSerializer.Serialize(account);
        Assert.DoesNotContain(account.Key!, json);
        Assert.DoesNotContain("SESSDATA", json);
        Assert.DoesNotContain("synthetic-a", json);
        Assert.DoesNotContain("Key", json);
    }
}
