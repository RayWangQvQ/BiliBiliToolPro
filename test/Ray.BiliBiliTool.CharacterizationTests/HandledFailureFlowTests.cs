using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Application;
using Ray.BiliBiliTool.CharacterizationTests.Support;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.CharacterizationTests;

[Collection("Characterization")]
public class HandledFailureFlowTests
{
    [Fact]
    public async Task MangaFlow_HandledSignFailure_ContinuesReadingAndReportsFailure()
    {
        var collector = new TestLogCollector();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ClearProviders().AddProvider(collector));
        services.AddOptions();
        services.Configure<MangaTaskOptions>(options => options.IsEnable = true);
        using var provider = services.BuildServiceProvider();
        Global.ServiceProviderRoot = provider;
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["BiliBiliCookies:0"] =
                            "DedeUserID=1;SESSDATA=test-sess;bili_jct=test-jct;buvid3=test-buvid",
                        ["PlatformType"] = "Web",
                    }
                )
                .Build();
            var read = false;
            var account = FlowProxy.Create<IAccountDomainService>(
                (name, _) =>
                    name == "LoginByCookie"
                        ? Task.FromResult(
                            new UserInfo
                            {
                                Wbi_img = new() { img_url = "", sub_url = "" },
                            }
                        )
                        : throw new InvalidOperationException(name)
            );
            var manga = FlowProxy.Create<IMangaDomainService>(
                (name, _) =>
                {
                    if (name == "MangaSign")
                        return Task.FromException(
                            new InvalidOperationException("test sign failure")
                        );
                    if (name == "MangaRead")
                    {
                        read = true;
                        return Task.CompletedTask;
                    }
                    throw new InvalidOperationException(name);
                }
            );
            var service = new MangaTaskAppService(
                provider.GetRequiredService<ILogger<MangaTaskAppService>>(),
                provider.GetRequiredService<IOptionsMonitor<MangaTaskOptions>>(),
                account,
                manga,
                null!,
                configuration,
                new CookieStrFactory<BiliCookie>(configuration)
            );
            await Assert.ThrowsAsync<AggregateException>(() => service.DoTaskForAccountAsync(1));
            Assert.True(read);
            Assert.Contains(
                collector.Entries,
                entry => entry.Message.Contains("FlowFailed 漫画任务")
            );
            Assert.DoesNotContain(
                collector.Entries,
                entry => entry.Message.Contains("FlowCompleted 漫画任务")
            );
        }
        finally
        {
            Global.ServiceProviderRoot = null;
        }
    }
}

public class FlowProxy : DispatchProxy
{
    private Func<string, object?[]?, object?> _invoke = null!;

    public static T Create<T>(Func<string, object?[]?, object?> invoke)
        where T : class
    {
        var result = Create<T, FlowProxy>();
        ((FlowProxy)(object)result)._invoke = invoke;
        return result;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args) =>
        _invoke(method!.Name, args);
}
