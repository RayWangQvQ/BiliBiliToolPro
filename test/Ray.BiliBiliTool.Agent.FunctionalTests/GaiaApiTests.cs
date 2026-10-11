using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class GaiaApiTests
{
    private readonly IGaiaApi _api;
    private readonly BiliCookie _ck;

    public GaiaApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _api = host.Services.GetRequiredService<IGaiaApi>();
        _ck = ExternalCookie.Require(host.Services);
    }

    #region GetDeviceFingerprintAsync

    [Fact]
    public async Task GetDeviceFingerprint_Normal_ReturnsFreshBuvidPair()
    {
        // Act
        BiliApiResponse<DeviceFingerprintResponse> re = await _api.GetDeviceFingerprint(
            _ck.ToString()
        );

        // Assert
        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data!.B_3.Should().NotBeNullOrWhiteSpace();
        re.Data.B_4.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region ReportDeviceFingerprint

    [Fact]
    public async Task ReportDeviceFingerprint_WithFreshBuvid_EnrollsDeviceProfile()
    {
        // Arrange: first obtain a fresh buvid3/buvid4 pair with no server-side profile
        var fp = await _api.GetDeviceFingerprint(_ck.ToString());
        fp.Code.Should().Be(0);
        fp.Data.Should().NotBeNull();

        // Build a cookie header carrying the fresh device values
        var ckHeader = BuildDeviceCookieHeader(fp.Data!.B_3!, fp.Data.B_4!);

        // Act: report the fingerprint to the gaia gateway to create the profile
        BiliApiResponse<object> re = await _api.ReportDeviceFingerprint(
            ckHeader,
            new GaiaReportRequest { Payload = GaiaDeviceFingerprintTemplate.PayloadJson }
        );

        // Assert: the report itself is accepted; risk-control outcome is verified in the
        // DeviceFingerprintEnrollmentTests, not here (it depends on account-level state).
        re.Should().NotBeNull();
        re.Code.Should().Be(0);
    }

    #endregion

    private static string BuildDeviceCookieHeader(string buvid3, string buvid4)
    {
        var parts = new List<string> { $"buvid3={buvid3}", $"buvid4={buvid4}" };
        return string.Join("; ", parts);
    }
}
