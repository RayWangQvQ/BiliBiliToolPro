using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Infrastructure;
using Refit;

namespace Ray.BiliBiliTool.Console.Extensions
{
    public static class RefitClientExtension
    {
        public static IServiceCollection AddBiliBiliClient<TInterface>(this IServiceCollection services, string host) where TInterface : class
        {
            var settings = new RefitSettings(new SystemTextJsonContentSerializer(JsonSerializerOptionsBuilder.DefaultOptions));

            services.AddRefitClient<TInterface>(settings)
                .ConfigureHttpClient((sp, c) =>
                {
                    c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<BiliBiliCookiesOptions>().ToString());
                    c.BaseAddress = new Uri(host);
                })
                .AddHttpMessageHandler(sp => new MyHttpClientDelegatingHandler(sp.GetRequiredService<ILogger<MyHttpClientDelegatingHandler>>()));

            return services;
        }
    }
}
