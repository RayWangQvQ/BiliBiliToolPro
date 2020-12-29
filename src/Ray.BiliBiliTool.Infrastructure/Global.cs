using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Infrastructure
{
    public class Global
    {
        /// <summary>
        /// 环境Key
        /// </summary>
        public static string EnvironmentKey = "ASPNETCORE_ENVIRONMENT";

        /// <summary>
        /// 环境
        /// </summary>
        public static IHostEnvironment HostingEnvironment { get; set; }

        /// <summary>
        /// 根配置
        /// </summary>
        public static IConfigurationRoot ConfigurationRoot { get; set; }

        /// <summary>
        /// 根容器
        /// </summary>
        public static IServiceProvider ServiceProviderRoot { get; set; }
    }
}
