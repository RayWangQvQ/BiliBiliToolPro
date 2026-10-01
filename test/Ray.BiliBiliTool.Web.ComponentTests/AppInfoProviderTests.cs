using System.Reflection;
using FluentAssertions;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class AppInfoProviderTests
{
    [Fact]
    public void AppVersion_GivenAssembly_ReadsItsDisplayVersion()
    {
        var assembly = typeof(AppVersion).Assembly;
        var provider = new AppInfoProvider(assembly);

        provider.AppVersion.Should().Be(AppVersion.DisplayOf(assembly));
    }

    [Fact]
    public void AppVersion_ApplicationAssembly_DoesNotExposePlaceholderVersion()
    {
        var provider = new AppInfoProvider(typeof(AppVersion).Assembly);

        provider.AppVersion.Should().NotBeNullOrWhiteSpace();
        provider.AppVersion.Should().NotBe(AppVersion.LocalBuildVersion);
    }

    [Fact]
    public void Constructor_WithoutAssembly_DoesNotThrow()
    {
        var provider = new AppInfoProvider();

        provider.AppVersion.Should().NotBeNullOrWhiteSpace();
    }
}
