using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Serilog;

namespace Ray.BiliBiliTool.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PreWorks(args);

            StartRun();

            //如果配置了“1”就立即关闭，否则保持窗口以便查看日志信息
            if (Global.ConfigurationRoot["CloseConsoleWhenEnd"] == "1") return;
            else System.Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="args"></param>
        public static void PreWorks(string[] args)
        {
            IHostBuilder hostBuilder = new HostBuilder();

            //承载系统自身的配置：
            hostBuilder.ConfigureHostConfiguration(hostConfigurationBuilder =>
            {
                hostConfigurationBuilder.AddJsonFile("commandLineMappings.json", false, false);

                Environment.SetEnvironmentVariable(HostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable(Global.EnvironmentKey));
                hostConfigurationBuilder.AddEnvironmentVariables();
            });

            //应用配置:
            hostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                Global.HostingEnvironment = hostBuilderContext.HostingEnvironment;
                configurationBuilder.AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{hostBuilderContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddJsonFile("exp.json", false, true)
                    .AddJsonFile("donateCoinCanContinueStatus.json", false, true)
                    .AddExcludeEmptyEnvironmentVariables("Ray_");
                if (args != null && args.Length > 0)
                {
                    configurationBuilder.AddCommandLine(args, hostBuilderContext.Configuration
                        .GetSection("CommandLineMappings")
                        .Get<Dictionary<string, string>>());
                }
            });

            //日志:
            hostBuilder.ConfigureLogging((hostBuilderContext, loggingBuilder) =>
            {
                Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostBuilderContext.Configuration)
                .CreateLogger();
            }).UseSerilog();

            //DI容器:
            hostBuilder.ConfigureServices((hostContext, services) =>
            {
                Global.ConfigurationRoot = (IConfigurationRoot)hostContext.Configuration;

                services.AddBiliBiliConfigs(hostContext.Configuration);
                services.AddBiliBiliClientApi(hostContext.Configuration);
                services.AddDomainServices();
                services.AddAppServices();
            });

            IHost host = hostBuilder.UseConsoleLifetime().Build();

            Global.ServiceProviderRoot = host.Services;
        }

        /// <summary>
        /// 开始运行
        /// </summary>
        private static void StartRun()
        {
            using IServiceScope serviceScope = Global.ServiceProviderRoot.CreateScope();
            IServiceProvider di = serviceScope.ServiceProvider;

            ILogger<Program> logger = di.GetRequiredService<ILogger<Program>>();
            LogAppInfo(logger);

            BiliBiliCookieOptions biliBiliCookieOptions = di.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue;
            biliBiliCookieOptions.Check(logger);//todo：使用配置检查

            IDailyTaskAppService dailyTask = di.GetRequiredService<IDailyTaskAppService>();

            try
            {
                dailyTask.DoDailyTask();
            }
            catch (Exception ex)
            {
                logger.LogError("程序异常终止，原因：{msg}", ex.Message);
            }
            finally
            {
                logger.LogInformation("开始推送");
            }
        }

        /// <summary>
        /// 打印应用信息
        /// </summary>
        /// <param name="logger"></param>
        private static void LogAppInfo(Microsoft.Extensions.Logging.ILogger logger)
        {
            logger.LogInformation(
                "版本号：{version}",
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "未知");
            logger.LogInformation("开源地址：{url}", Constants.SourceCodeUrl);
            logger.LogInformation("当前环境：{env} \r\n", Global.HostingEnvironment.EnvironmentName ?? "无");
        }
    }
}
