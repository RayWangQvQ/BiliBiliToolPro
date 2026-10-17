using System;
using System.Collections.Generic;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Domain.Exceptions;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class BiliCookieTests
{
    // --- CookieInfo.Check() via BiliCookie (base validation) ---

    [Fact]
    public void Check_EmptyDictionary_ThrowsBiliValidationException()
    {
        var cookie = new BiliCookie(new Dictionary<string, string>());
        Assert.Throws<BiliValidationException>(() => cookie.Check());
    }

    // --- BiliCookie-specific validation ---

    [Fact]
    public void Check_MissingUserId_ThrowsBiliValidationException()
    {
        var cookie = new BiliCookie(
            new Dictionary<string, string> { { "bili_jct", "abc" }, { "SESSDATA", "xyz" } }
        );
        Assert.Throws<BiliValidationException>(() => cookie.Check());
    }

    [Fact]
    public void Check_NonNumericUserId_ThrowsBiliValidationException()
    {
        var cookie = new BiliCookie(
            new Dictionary<string, string>
            {
                { "DedeUserID", "notanumber" },
                { "bili_jct", "abc" },
                { "SESSDATA", "xyz" },
            }
        );
        Assert.Throws<BiliValidationException>(() => cookie.Check());
    }

    [Fact]
    public void Check_MissingSessData_ThrowsBiliValidationException()
    {
        var cookie = new BiliCookie(
            new Dictionary<string, string> { { "DedeUserID", "12345" }, { "bili_jct", "abc" } }
        );
        Assert.Throws<BiliValidationException>(() => cookie.Check());
    }

    [Fact]
    public void Check_MissingBiliJct_ThrowsBiliValidationException()
    {
        var cookie = new BiliCookie(
            new Dictionary<string, string> { { "DedeUserID", "12345" }, { "SESSDATA", "xyz" } }
        );
        Assert.Throws<BiliValidationException>(() => cookie.Check());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9223372036854775807")]
    public void Check_ValidNumericUserId_DoesNotThrow(string userId)
    {
        var cookie = new BiliCookie(
            new Dictionary<string, string>
            {
                ["DedeUserID"] = userId,
                ["bili_jct"] = "csrf",
                ["SESSDATA"] = "session",
            }
        );

        cookie.Check();
        Assert.Equal(userId, cookie.UserId);
    }

    [Fact]
    public void Check_OverflowingUserId_ThrowsBiliValidationException()
    {
        var cookie = new BiliCookie(
            new Dictionary<string, string>
            {
                ["DedeUserID"] = "9223372036854775808",
                ["bili_jct"] = "csrf",
                ["SESSDATA"] = "session",
            }
        );

        Assert.Throws<BiliValidationException>(() => cookie.Check());
    }

    // --- BiliResiliencePolicies constant assertions ---

    [Fact]
    public void ReadOnlyRetryCount_Default_IsOne()
    {
        Assert.Equal(1, BiliResiliencePolicies.ReadOnlyRetryCount);
    }

    [Fact]
    public void HttpTimeout_Default_IsThirtySeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), BiliResiliencePolicies.HttpTimeout);
    }

    [Fact]
    public void ReadOnlyRetryBackoff_Default_IsTwoSeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), BiliResiliencePolicies.ReadOnlyRetryBackoff);
    }
}
