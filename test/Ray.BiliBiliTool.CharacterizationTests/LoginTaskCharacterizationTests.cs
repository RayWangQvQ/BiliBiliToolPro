using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.CharacterizationTests.Support;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.CharacterizationTests;

public class LoginTaskCharacterizationTests
{
    [Fact]
    public async Task Login_flow_preserves_current_step_order_and_emits_diagnostics()
    {
        var callLog = new List<string>();
        using var logging = TestLoggingContext.Create();

        var initialCookie = CreateCookie("101");
        var setCookie = CreateCookie("101", includeBuvid: true);
        var loginDomainService = new LoginDomainServiceDouble(callLog, initialCookie, setCookie);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PlatformType"] = "Web" })
            .Build();

        var service = new Ray.BiliBiliTool.Application.LoginTaskAppService(
            configuration,
            logging.LoggerFactory.CreateLogger<Ray.BiliBiliTool.Application.LoginTaskAppService>(),
            loginDomainService
        );

        await service.DoTaskAsync();

        callLog.Should().Equal("LoginByQrCodeAsync", "SetCookieAsync", "SaveCookieToJsonFileAsync");

        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("FlowStart LoginTask"));
        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("FlowCompleted LoginTask"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("扫码登录"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("获取二维码"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("Set Cookie"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("持久化Cookie"));
    }

    private static BiliCookie CreateCookie(string userId, bool includeBuvid = false)
    {
        var values = new Dictionary<string, string>
        {
            ["DedeUserID"] = userId,
            ["SESSDATA"] = $"sess-{userId}",
            ["bili_jct"] = $"jct-{userId}",
        };

        if (includeBuvid)
        {
            values["buvid3"] = $"buvid-{userId}";
        }

        return new BiliCookie(values);
    }

    private sealed class LoginDomainServiceDouble(
        List<string> callLog,
        BiliCookie loginCookie,
        BiliCookie setCookieResult
    ) : ILoginDomainService
    {
        public Task<BiliCookie> LoginByQrCodeAsync(CancellationToken cancellationToken)
        {
            callLog.Add(nameof(LoginByQrCodeAsync));
            return Task.FromResult(loginCookie);
        }

        public Task<BiliCookie> SetCookieAsync(
            BiliCookie cookie,
            CancellationToken cancellationToken
        )
        {
            callLog.Add(nameof(SetCookieAsync));
            return Task.FromResult(setCookieResult);
        }

        public Task SaveCookieToJsonFileAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            callLog.Add(nameof(SaveCookieToJsonFileAsync));
            return Task.CompletedTask;
        }

        public Task<bool> SaveCookieToQinLongAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<bool> SaveCookieToBaihuAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<QrLoginGenerateResult> GenerateQrCodeWebAsync(
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<QrLoginCheckResult> CheckQrLoginAsync(
            string qrcodeKey,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestLoggingContext(
        IServiceProvider serviceProvider,
        TestLogCollector collector
    ) : IDisposable
    {
        public ILoggerFactory LoggerFactory => serviceProvider.GetRequiredService<ILoggerFactory>();

        public TestLogCollector Collector { get; } = collector;

        public static TestLoggingContext Create()
        {
            var collector = new TestLogCollector();
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.ClearProviders().AddProvider(collector));

            var serviceProvider = services.BuildServiceProvider();
            Global.ServiceProviderRoot = serviceProvider;

            return new TestLoggingContext(serviceProvider, collector);
        }

        public void Dispose()
        {
            Global.ServiceProviderRoot = null;
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
