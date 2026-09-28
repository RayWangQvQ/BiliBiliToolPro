#nullable enable

using Ray.BiliBiliTool.Config;
using Xunit;

namespace ConfigTest;

/// <summary>
/// 「应用版本」的展示规则：CI 产物显示版本号，非 CI 产物统一折叠成兜底文案。
/// </summary>
public class AppVersionTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("0.0.0-dev", true)]
    [InlineData("4.0.8", false)]
    [InlineData("4.0.8-alpha.3", false)]
    public void IsLocalBuild_JudgesByInformationalVersion(
        string? informationalVersion,
        bool expected
    ) => Assert.Equal(expected, AppVersion.IsLocalBuild(informationalVersion));

    [Fact]
    public void DisplayOf_RealAssembly_NeverShowsPlaceholderVersion()
    {
        var assembly = typeof(AppVersion).Assembly;

        var display = AppVersion.DisplayOf(assembly);

        Assert.False(string.IsNullOrWhiteSpace(display));
        Assert.NotEqual(AppVersion.LocalBuildVersion, display);
    }

    [Fact]
    public void DisplayOf_RealAssembly_MatchesInformationalWhenBuiltByCi()
    {
        var assembly = typeof(AppVersion).Assembly;
        var informational = AppVersion.InformationalOf(assembly);

        var display = AppVersion.DisplayOf(assembly);

        var expected = AppVersion.IsLocalBuild(informational)
            ? AppVersion.LocalBuildDisplay
            : informational;
        Assert.Equal(expected, display);
    }

    [Fact]
    public void InformationalOf_TrimsCommitSuffix()
    {
        // 真实程序集的 InformationalVersion 可能带 +commit，展示值里不应出现
        var informational = AppVersion.InformationalOf(typeof(AppVersion).Assembly);

        if (informational is not null)
            Assert.DoesNotContain("+", informational);
    }

    [Fact]
    public void LocalBuildVersion_MatchesCommonPropsFallback()
    {
        // common.props 里本地构建的兜底值；改了 common.props 这里会红
        Assert.Equal("0.0.0-dev", AppVersion.LocalBuildVersion);
    }
}
