using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.DaiDai;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

/// <summary>
/// Covers the device fingerprint enrollment added to <see cref="LoginDomainService.SetCookieAsync"/>:
/// an already enrolled device (buvid3 + _uuid) is left alone, a missing buvid3 is generated through
/// finger/spi and then reported to gaia once, and any report failure is swallowed so the Cookie survives.
/// </summary>
public class DeviceFingerprintEnrollmentTests
{
    [Fact]
    public async Task SetCookieAsync_DoesNotReport_WhenDeviceAlreadyEnrolled()
    {
        var cookie = CreateCookie();
        cookie.CookieItemDictionary["_uuid"] = "existing-uuid";
        var gaiaApi = Proxy.Create<IGaiaApi>(
            (method, _) => throw new InvalidOperationException($"gaia must not be called: {method}")
        );
        var service = CreateService(gaiaApi);

        await service.SetCookieAsync(cookie, CancellationToken.None);

        Assert.Equal("buvid", cookie.CookieItemDictionary["buvid3"]);
        Assert.Equal("existing-uuid", cookie.CookieItemDictionary["_uuid"]);
    }

    [Fact]
    public async Task SetCookieAsync_GeneratesThenReportsAndWritesUuid_WhenBuvid3Missing()
    {
        var cookie = CreateCookie();
        cookie.CookieItemDictionary.Remove("buvid3");
        var calls = new List<string>();
        string? reportedCookieHeader = null;
        string? reportedPayload = null;
        var gaiaApi = Proxy.Create<IGaiaApi>(
            (method, args) =>
            {
                calls.Add(method);
                if (method == "GetDeviceFingerprint")
                {
                    return Task.FromResult(
                        new BiliApiResponse<DeviceFingerprintResponse>
                        {
                            Code = 0,
                            Data = new DeviceFingerprintResponse
                            {
                                B_3 = "fresh-buvid3",
                                B_4 = "fresh-buvid4",
                            },
                        }
                    );
                }

                if (method == "ReportDeviceFingerprint")
                {
                    reportedCookieHeader = (string)args[0]!;
                    reportedPayload = ((GaiaReportRequest)args[1]!).Payload;
                    return Task.FromResult(new BiliApiResponse<object> { Code = 0 });
                }

                throw new InvalidOperationException(method);
            }
        );
        var service = CreateService(gaiaApi);

        await service.SetCookieAsync(cookie, CancellationToken.None);

        // finger/spi must run before the gaia report.
        Assert.Equal(new[] { "GetDeviceFingerprint", "ReportDeviceFingerprint" }, calls);
        Assert.Equal("fresh-buvid3", cookie.CookieItemDictionary["buvid3"]);
        Assert.Equal("fresh-buvid4", cookie.CookieItemDictionary["buvid4"]);
        var deviceUuid = cookie.CookieItemDictionary.GetValueOrDefault("_uuid");
        Assert.False(string.IsNullOrWhiteSpace(deviceUuid));

        // The report must carry the freshly generated device and the device Cookie header.
        Assert.NotNull(reportedCookieHeader);
        Assert.NotNull(reportedPayload);
        Assert.Contains("buvid3=fresh-buvid3", reportedCookieHeader!);
        Assert.Contains($"_uuid={deviceUuid}", reportedCookieHeader!);
        Assert.Contains($"\"df35\":\"{deviceUuid}\"", reportedPayload!);
    }

    [Fact]
    public async Task SetCookieAsync_SwallowsNonZeroReport_AndKeepsCookie()
    {
        var cookie = CreateCookie();
        var gaiaApi = Proxy.Create<IGaiaApi>(
            (method, _) =>
            {
                if (method == "ReportDeviceFingerprint")
                {
                    return Task.FromResult(
                        new BiliApiResponse<object> { Code = -403, Message = "denied" }
                    );
                }

                throw new InvalidOperationException(method);
            }
        );
        var service = CreateService(gaiaApi);

        // A failed report is only logged, it must not bubble up.
        await service.SetCookieAsync(cookie, CancellationToken.None);

        Assert.Equal("buvid", cookie.CookieItemDictionary["buvid3"]);
        Assert.Equal("buvid4", cookie.CookieItemDictionary["buvid4"]);
        Assert.False(cookie.CookieItemDictionary.ContainsKey("_uuid"));
    }

    [Fact]
    public async Task SetCookieAsync_SwallowsReportException_AndKeepsCookie()
    {
        var cookie = CreateCookie();
        var gaiaApi = Proxy.Create<IGaiaApi>(
            (method, _) =>
            {
                if (method == "ReportDeviceFingerprint")
                {
                    throw new HttpRequestException("network down");
                }

                throw new InvalidOperationException(method);
            }
        );
        var service = CreateService(gaiaApi);

        // A thrown report is only logged, it must not bubble up.
        await service.SetCookieAsync(cookie, CancellationToken.None);

        Assert.Equal("buvid", cookie.CookieItemDictionary["buvid3"]);
        Assert.Equal("buvid4", cookie.CookieItemDictionary["buvid4"]);
        Assert.False(cookie.CookieItemDictionary.ContainsKey("_uuid"));
    }

    private static LoginDomainService CreateService(
        IGaiaApi gaiaApi,
        DeviceCookieOptions? deviceCookieOptions = null
    ) =>
        new(
            NullLogger<LoginDomainService>.Instance,
            Proxy.Create<IPassportApi>(Throw),
            new TestHostEnvironment(),
            Proxy.Create<IQingLongApi>(Throw),
            Proxy.Create<IBaihuApi>(Throw),
            Proxy.Create<IDaiDaiApi>(Throw),
            Proxy.Create<IHomeApi>(
                (method, _) =>
                {
                    if (method == "GetHomePageAsync")
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                    }

                    throw new InvalidOperationException(method);
                }
            ),
            gaiaApi,
            new ConfigurationBuilder().Build(),
            Options.Create(new QingLongOptions()),
            Options.Create(new BaihuOptions()),
            Options.Create(new DaiDaiOptions()),
            Options.Create(deviceCookieOptions ?? new DeviceCookieOptions())
        );

    private static object Throw(string method, object?[] args) =>
        throw new InvalidOperationException(method);

    private static BiliCookie CreateCookie() =>
        new(
            new Dictionary<string, string>
            {
                ["DedeUserID"] = "123",
                ["SESSDATA"] = "sess",
                ["bili_jct"] = "csrf",
                ["buvid3"] = "buvid",
                ["buvid4"] = "buvid4",
            }
        );

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(DeviceFingerprintEnrollmentTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    public class Proxy : DispatchProxy
    {
        public Func<string, object?[], object> Handler { get; set; } = null!;

        protected override object Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method!.Name, args ?? []);

        public static T Create<T>(Func<string, object?[], object> handler)
            where T : class
        {
            var value = Create<T, Proxy>();
            ((Proxy)(object)value).Handler = handler;
            return value;
        }
    }
}
