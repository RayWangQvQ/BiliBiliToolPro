using System;
using System.Collections.Generic;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Domain.Exceptions;
using Xunit;

namespace BiliAgentTest;

public class BiliCookieExceptionTests
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

    // --- BiliResiliencePolicies constant assertions ---

    [Fact]
    public void BiliResiliencePolicies_ReadOnlyRetryCount_IsOne()
    {
        Assert.Equal(1, BiliResiliencePolicies.ReadOnlyRetryCount);
    }

    [Fact]
    public void BiliResiliencePolicies_HttpTimeout_Is30Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), BiliResiliencePolicies.HttpTimeout);
    }

    [Fact]
    public void BiliResiliencePolicies_ReadOnlyRetryBackoff_Is2Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), BiliResiliencePolicies.ReadOnlyRetryBackoff);
    }
}
