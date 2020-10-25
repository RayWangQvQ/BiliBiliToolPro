using System;
using System.Collections.Generic;
using System.Text;
using BiliBiliTool.Login;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console.Agent.Interfaces;
using Refit;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Ray.BiliBiliTool.Console
{
    public static class RefitClientExtension
    {
        public static IServiceCollection AddBiliBiliClient<TInterface>(this IServiceCollection services, string host) where TInterface : class
        {
            var settings = new RefitSettings(new SystemTextJsonContentSerializer(JsonSerializerOptionsBuilder.DefaultOptions));

            services.AddRefitClient<TInterface>(settings)
                .ConfigureHttpClient((sp, c) =>
                {
                    c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<Verify>().getVerify());
                    c.BaseAddress = new Uri(host);
                })
                .AddHttpMessageHandler(sp => new MyHttpClientDelegatingHandler(sp.GetRequiredService<ILogger<MyHttpClientDelegatingHandler>>()));

            return services;
        }
    }
}
