using System;
using System.Collections.Generic;
using System.Text.Json;
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
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            PreWorks(args);

            using (var serviceScope = RayContainer.Root.CreateScope())
            {
                ILogger logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("-----任务启动-----");

                BiliBiliCookiesOptions biliBiliCookiesOptions = serviceScope.ServiceProvider.GetRequiredService<IOptionsMonitor<BiliBiliCookiesOptions>>().CurrentValue;
                if (!biliBiliCookiesOptions.Check(logger)) return;

                if (args.Length > 3)
                {
                    //ServerVerify.verifyInit(args[3]);
                }

                //每日任务65经验
                IDailyTaskAppService dailyTask = serviceScope.ServiceProvider.GetRequiredService<IDailyTaskAppService>();
                dailyTask.DoDailyTask();

                logger.LogInformation("-----任务结束-----");
            }

            //System.Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="args"></param>
        public static void PreWorks(string[] args)
        {
            var mapper = new Dictionary<string, string>
            {
                {"-userId","BiliBiliCookies:UserId" },
                {"-sessData","BiliBiliCookies:SessData" },
                {"-biliJct","BiliBiliCookies:BiliJct" },
            };

            RayConfiguration.Root = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args, mapper)
                //.AddJsonFile("appsettings.local.json", true)
                .Build();

            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServices(services);
                })
                .UseConsoleLifetime();

            RayContainer.Root = hostBuilder.Build().Services;
        }

        /// <summary>
        /// 注册容器
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureServices(IServiceCollection services)
        {
            //配置
            services.AddSingleton<IConfiguration>(RayConfiguration.Root);
            services.AddBiliBiliConfigs(RayConfiguration.Root);

            //日志
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(RayConfiguration.Root.GetSection("Logging"))
                    .AddConsole()
                    .AddDebug();
            });

            services.AddBiliBiliClientApi();
            services.AddDomainServices();
            services.AddAppServices();
        }
    }
}
