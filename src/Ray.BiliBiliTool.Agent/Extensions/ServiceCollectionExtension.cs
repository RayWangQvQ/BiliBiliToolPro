using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Ray.BiliBiliTool.Agent.ServerChanAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;
using Refit;

namespace Ray.BiliBiliTool.Agent.Extensions
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// 注册强类型api客户端
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBiliBiliClientApi(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddHttpClient("BiliBiliWithCookie",
                (sp, c) =>
                {
                    c.DefaultRequestHeaders.Add("Cookie",
                        sp.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue.ToString());
                    c.DefaultRequestHeaders.Add("User-Agent",
                        sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent);
                });

            //bilibli
            services.AddBiliBiliClientApi<IDailyTaskApi>("https://api.bilibili.com");
            services.AddBiliBiliClientApi<IMangaApi>("https://manga.bilibili.com");
            services.AddBiliBiliClientApi<IAccountApi>("https://account.bilibili.com");
            services.AddBiliBiliClientApi<ILiveApi>("https://api.live.bilibili.com");
            services.AddBiliBiliClientApi<IRelationApi>("https://api.bilibili.com/x/relation");

            //server酱推送
            services.AddRefitClient<IPushApi>(new RefitSettings(new SystemTextJsonContentSerializer(JsonSerializerOptionsBuilder.DefaultOptions)))
                .ConfigureHttpClient((sp, c) =>
                {
                    c.BaseAddress = new Uri("http://sc.ftqq.com");
                });
            services.AddScoped<PushService>();

            return services;
        }

        /// <summary>
        /// 封装Refit，默认将Cookie添加到Header中
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="services"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IServiceCollection AddBiliBiliClientApi<TInterface>(this IServiceCollection services, string host) where TInterface : class
        {
            var settings = new RefitSettings(new SystemTextJsonContentSerializer(JsonSerializerOptionsBuilder.DefaultOptions));

            services.AddRefitClient<TInterface>(settings)
                .ConfigureHttpClient((sp, c) =>
                {
                    c.DefaultRequestHeaders.Add("Cookie",
                        sp.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue.ToString());
                    c.DefaultRequestHeaders.Add("User-Agent",
                        sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent);
                    c.BaseAddress = new Uri(host);
                })
                .AddHttpMessageHandler(sp => new MyHttpClientDelegatingHandler(
                    sp.GetRequiredService<ILogger<MyHttpClientDelegatingHandler>>(),
                    sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>()
                    ));

            return services;
        }
    }
}
