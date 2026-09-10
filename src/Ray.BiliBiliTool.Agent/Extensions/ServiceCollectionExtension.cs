using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Refit;

namespace Ray.BiliBiliTool.Agent.Extensions;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// 注册强类型api客户端
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBiliBiliClientApi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        //Cookie
        services.AddSingleton<CookieStrFactory<BiliCookie>>();

        //全局代理
        services.SetGlobalProxy(configuration);

        //DelegatingHandler
        services.Scan(scan =>
            scan.FromAssemblyOf<BiliBiliCommonHeadersDelegatingHandler>()
                .AddClasses(classes => classes.AssignableTo<DelegatingHandler>())
                .AsSelf()
                .WithTransientLifetime()
        );

        //服务
        services.AddScoped<IWbiService, WbiService>();

        //bilibli
        Action<IServiceProvider, HttpClient> config = (sp, c) =>
        {
            c.DefaultRequestHeaders.Add(
                "User-Agent",
                sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent
            );
            c.Timeout = BiliResiliencePolicies.HttpTimeout;
        };
        Action<IServiceProvider, HttpClient> configApp = (sp, c) =>
        {
            c.DefaultRequestHeaders.Add(
                "User-Agent",
                sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgentApp
            );
            c.Timeout = BiliResiliencePolicies.HttpTimeout;
        };

        services.AddBiliBiliClientApi<INavApi>(BiliHosts.Api, config, true);

        services.AddBiliBiliClientApi<IApiApi>(
            BiliHosts.Api,
            config,
            policy: BiliResiliencePolicies.MutatingPolicy()
        );

        services.AddBiliBiliClientApi<IShowApi>(BiliHosts.Show, config);
        services.AddBiliBiliClientApi<IPassportApi>(BiliHosts.Passport, config);
        services.AddBiliBiliClientApi<ILiveTraceApi>(BiliHosts.LiveTrace, config);
        services.AddBiliBiliClientApi<IHomeApi>(BiliHosts.Www, config);
        services.AddBiliBiliClientApi<IMangaApi>(BiliHosts.Manga, config);
        services.AddBiliBiliClientApi<IAccountApi>(BiliHosts.Account, config);
        services.AddBiliBiliClientApi<ILiveApi>(
            BiliHosts.Live,
            config,
            policy: BiliResiliencePolicies.MutatingPolicy()
        );

        //qinglong
        var qinglongHost = configuration["QL_URL"] ?? "http://localhost:5600";
        services
            .AddRefitClient<IQingLongApi>()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    c.BaseAddress = new Uri(qinglongHost);
                    c.DefaultRequestHeaders.Add(
                        "User-Agent",
                        sp.GetRequiredService<
                            IOptionsMonitor<SecurityOptions>
                        >().CurrentValue.UserAgent
                    );
                    c.Timeout = BiliResiliencePolicies.HttpTimeout;
                }
            )
            .AddPolicyHandler(BiliResiliencePolicies.ReadOnlyPolicy());

        //baihu
        var baihuHost = configuration["BA_URL"] ?? "http://localhost:8052";
        services
            .AddRefitClient<IBaihuApi>()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    c.BaseAddress = new Uri(baihuHost);
                    c.DefaultRequestHeaders.Add(
                        "User-Agent",
                        sp.GetRequiredService<
                            IOptionsMonitor<SecurityOptions>
                        >().CurrentValue.UserAgent
                    );
                }
            )
            .AddPolicyHandler(BiliResiliencePolicies.ReadOnlyPolicy());

        return services;
    }

    /// <summary>
    /// 封装Refit，默认将Cookie添加到Header中
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <param name="services"></param>
    /// <param name="host"></param>
    /// <returns></returns>
    private static IServiceCollection AddBiliBiliClientApi<TInterface>(
        this IServiceCollection services,
        string host,
        Action<IServiceProvider, HttpClient> config,
        bool ignorWrid = false,
        IAsyncPolicy<HttpResponseMessage>? policy = null
    )
        where TInterface : class
    {
        IHttpClientBuilder httpClientBuilder = services
            .AddRefitClient<TInterface>()
            .ConfigureHttpClient((_, c) => c.BaseAddress = new Uri(host))
            .ConfigureHttpClient(config)
            .AddHttpMessageHandler<LogDelegatingHandler>()
            .AddHttpMessageHandler<BiliBiliCommonHeadersDelegatingHandler>()
            .AddHttpMessageHandler<IntervalDelegatingHandler>()
            .AddPolicyHandler(policy ?? BiliResiliencePolicies.ReadOnlyPolicy());

        if (!ignorWrid)
        {
            httpClientBuilder.AddHttpMessageHandler<WridEncryptionDelegatingHandler>();
        }

        return services;
    }

    /// <summary>
    /// 设置全局代理(如果配置了代理)
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IServiceCollection SetGlobalProxy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var proxyAddress = configuration["Security:WebProxy"];
        if (!string.IsNullOrWhiteSpace(proxyAddress))
        {
            WebProxy webProxy = new WebProxy();

            //user:password@host:port http proxy only .Tested with tinyproxy-1.11.0-rc1
            if (proxyAddress!.Contains("@"))
            {
                string userPass = proxyAddress.Split("@")[0];
                string address = proxyAddress.Split("@")[1];

                string proxyUser = "";
                string proxyPass = "";
                if (userPass.Contains(":"))
                {
                    proxyUser = userPass.Split(":")[0];
                    proxyPass = userPass.Split(":")[1];
                }

                webProxy.Address = new Uri("http://" + address);
                webProxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
            }
            else
            {
                webProxy.Address = new Uri(proxyAddress);
            }

            HttpClient.DefaultProxy = webProxy;
        }

        return services;
    }
}
