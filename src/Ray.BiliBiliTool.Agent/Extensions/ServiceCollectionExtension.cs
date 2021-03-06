using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Agent.Extensions
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// 注册强类型api客户端
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBiliBiliClientApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<BiliCookie>();

            //全局代理
            services.SetGlobalProxy(configuration);

            //DelegatingHandler
            services.Scan(scan => scan
                .FromAssemblyOf<IBiliBiliApi>()
                .AddClasses(classes => classes.AssignableTo<DelegatingHandler>())
                .AsSelf()
                .WithTransientLifetime()
            );

            //bilibli
            services.AddBiliBiliClientApi<IDailyTaskApi>("https://api.bilibili.com");
            services.AddBiliBiliClientApi<IMangaApi>("https://manga.bilibili.com");
            services.AddBiliBiliClientApi<IAccountApi>("https://account.bilibili.com");
            services.AddBiliBiliClientApi<ILiveApi>("https://api.live.bilibili.com");
            services.AddBiliBiliClientApi<IRelationApi>("https://api.bilibili.com");
            services.AddBiliBiliClientApi<IChargeApi>("https://api.bilibili.com");
            services.AddBiliBiliClientApi<IUserInfoApi>("https://api.bilibili.com");
            services.AddBiliBiliClientApi<IVideoApi>("https://api.bilibili.com");
            services.AddBiliBiliClientApi<IVideoWithoutCookieApi>("https://api.bilibili.com", false);

            return services;
        }

        /// <summary>
        /// 封装Refit，默认将Cookie添加到Header中
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="services"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static IServiceCollection AddBiliBiliClientApi<TInterface>(this IServiceCollection services, string host, bool withCookie = true)
            where TInterface : class
        {
            var uri = new Uri(host);
            var handler = services
                .AddHttpApi<TInterface>(o =>
                {
                    o.HttpHost = uri;
                    o.UseDefaultUserAgent = false;
                })
                .ConfigureHttpClient((sp, c) =>
                {
                    c.DefaultRequestHeaders.Add("User-Agent",
                        sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent);
                })
                .AddHttpMessageHandler<IntervalDelegatingHandler>()
                .AddPolicyHandler(GetRetryPolicy());

            if (withCookie)
                handler.ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var handler = new HttpClientHandler
                    {
                        CookieContainer = sp.GetRequiredService<BiliCookie>().CreateCookieContainer(uri)
                    };
                    return handler;
                });

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

                    string proxyUser = userPass.Split(":")[0];
                    string proxyPass = userPass.Split(":")[1];

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
}
