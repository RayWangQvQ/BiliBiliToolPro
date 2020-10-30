using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Config.Extensions
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// 注册配置
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBiliBiliConfigs(this IServiceCollection services, IConfiguration configuration)
        {
            //Options
            services.AddOptions()
                .Configure<JsonSerializerOptions>(o => o = JsonSerializerOptionsBuilder.DefaultOptions)
                .Configure<DailyTaskOptions>(configuration.GetSection("DailyTaskConfig"))
                .Configure<BiliBiliCookiesOptions>(configuration.GetSection("BiliBiliCookies"));

            return services;
        }
    }
}
