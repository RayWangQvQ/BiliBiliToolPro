using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Agent.Extensions;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// 注册强类型api客户端
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBiliBiliClientApi(this IServiceCollection services, IConfiguration configuration)
    {
        //Cookie
        services.AddSingleton<CookieStrFactory>(sp =>
        {
            var list = new List<string>();
            var config = sp.GetRequiredService<IConfiguration>();

            //兼容老版
            var old = config["BiliBiliCookie:CookieStr"];
            if (!string.IsNullOrWhiteSpace(old)) list.Add(old);

            var configList = config.GetSection("BiliBiliCookies")
                .Get<List<string>>() ?? new List<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            list.AddRange(configList);

            return new CookieStrFactory(list);
        });
        services.AddTransient<BiliCookie>();

        //全局代理
        services.SetGlobalProxy(configuration);

        //DelegatingHandler
        services.Scan(scan => scan
            .FromAssemblyOf<IBiliBiliApi>()
            .AddClasses(classes => classes.AssignableTo<DelegatingHandler>())
            .AsSelf()
            .WithTransientLifetime()
        );

        //服务
        services.AddScoped<IWbiService, WbiService>();

        //bilibli
        Action<IServiceProvider, HttpClient> config = (sp, c) => {
            c.DefaultRequestHeaders.Add("User-Agent", sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent);
            c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<BiliCookie>().ToString());
        };
        Action<IServiceProvider, HttpClient> configApp = (sp, c) => {
            c.DefaultRequestHeaders.Add("User-Agent", sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgentApp);
            c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<BiliCookie>().ToString());
        };

        services.AddBiliBiliClientApi<IUserInfoApi>(BiliHosts.Api, config);
        services.AddBiliBiliClientApi<IDailyTaskApi>(BiliHosts.Api, config);
        services.AddBiliBiliClientApi<IRelationApi>(BiliHosts.Api, config);
        services.AddBiliBiliClientApi<IChargeApi>(BiliHosts.Api, config);
        services.AddBiliBiliClientApi<IVideoApi>(BiliHosts.Api, config);
        services.AddBiliBiliClientApi<IVideoWithoutCookieApi>(BiliHosts.Api, config);
        services.AddBiliBiliClientApi<IArticleApi>(BiliHosts.Api, config);

        services.AddBiliBiliClientApi<IVipMallApi>(BiliHosts.Show, config);
        services.AddBiliBiliClientApi<IPassportApi>(BiliHosts.Passport, config);
        services.AddBiliBiliClientApi<ILiveTraceApi>(BiliHosts.LiveTrace,config);
        services.AddBiliBiliClientApi<IHomeApi>(BiliHosts.Www, config);
        services.AddBiliBiliClientApi<IMangaApi>(BiliHosts.Manga, config);
        services.AddBiliBiliClientApi<IAccountApi>(BiliHosts.Account, config);
        services.AddBiliBiliClientApi<ILiveApi>(BiliHosts.Live, config);

        services.AddBiliBiliClientApi<IVipBigPointApi>(BiliHosts.Api, configApp);


        //qinglong
        var qinglongHost = configuration["QL_URL"] ?? "http://localhost:5600";
        services
            .AddHttpApi<IQingLongApi>(o =>
            {
                o.HttpHost = new Uri(qinglongHost);
                o.UseDefaultUserAgent = false;
            })
            .ConfigureHttpClient((sp, c) =>
            {
                c.DefaultRequestHeaders.Add("User-Agent", sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    /// <summary>
    /// 封装Refit，默认将Cookie添加到Header中
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <param name="services"></param>
    /// <param name="host"></param>
    /// <returns></returns>
    private static IServiceCollection AddBiliBiliClientApi<TInterface>(this IServiceCollection services, string host, Action<IServiceProvider, HttpClient> config)
        where TInterface : class
    {
        var uri = new Uri(host);
        IHttpClientBuilder httpClientBuilder = services
            .AddHttpApi<TInterface>(o =>
            {
                o.HttpHost = uri;
                o.UseDefaultUserAgent = false;
            })
            .ConfigureHttpClient(config)
            .AddHttpMessageHandler<IntervalDelegatingHandler>()
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    /// <summary>
    /// 设置全局代理(如果配置了代理)
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IServiceCollection SetGlobalProxy(this IServiceCollection services, IConfiguration configuration)
    {
        string proxyAddress = configuration["Security:WebProxy"];
        if (proxyAddress.IsNotNullOrEmpty())
        {
            WebProxy webProxy = new WebProxy();

            //user:password@host:port http proxy only .Tested with tinyproxy-1.11.0-rc1
            if (proxyAddress.Contains("@"))
            {
                string userPass = proxyAddress.Split("@")[0];
                string address = proxyAddress.Split("@")[1];

                string proxyUser = "";
                string proxyPass = "";
                if (userPass.Contains(":"))
                {
                    proxyUser = userPass?.Split(":")[0];
                    proxyPass = userPass?.Split(":")[1];
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

    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                        retryAttempt)));
    }
}
