using System;
using Ray.BiliBiliTool.Infrastructure;

#nullable enable

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationBuilderExtension
    {
        /// <summary>
        /// 按环境变量名添加json配置
        /// </summary>
        /// <param name="configurationBuilder"></param>
        /// <param name="envName"></param>
        /// <param name="envprefix"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddJsonFileByEnv(
            this IConfigurationBuilder configurationBuilder,
            string envName = "ASPNETCORE_ENVIRONMENT",
            string envprefix = "")
        {
            envName = $"{envprefix}{envName}";
            string? env = Environment.GetEnvironmentVariable(envName);
            RayConfiguration.Env = env;

            configurationBuilder
                .AddJsonFile($"appsettings.{env}.json", true, false);//设置为可选、无监控

            return configurationBuilder;
        }
    }
}
