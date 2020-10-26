using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Application;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Console.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console
{
    public class Program
    {
        public static IConfigurationRoot ConfigurationRoot { get; set; }

        public static IServiceProvider ServiceProviderRoot { get; set; }


        static void Main(string[] args)
        {
            PreWorks(args);

            using (var serviceScope = ServiceProviderRoot.CreateScope())
            {
                ILogger logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                if (args.Length < 3)
                {
                    logger.LogInformation("-----任务启动失败-----");
                    logger.LogWarning("Cooikes参数缺失，请检查是否在Github Secrets中配置Cooikes参数");
                }

                if (args.Length > 3)
                {
                    //ServerVerify.verifyInit(args[3]);
                }

                //每日任务65经验
                logger.LogInformation("-----任务启动-----");

                IDailyTaskAppService dailyTask = serviceScope.ServiceProvider.GetRequiredService<IDailyTaskAppService>();
                dailyTask.DoDailyTask();
            }

            System.Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="verify"></param>
        public static void PreWorks(string[] args)
        {
            var mapper = new Dictionary<string, string>
            {
                {"-userId","BiliBiliCookies:UserId" },
                {"-sessData","BiliBiliCookies:SessData" },
                {"-biliJct","BiliBiliCookies:BiliJct" },
            };

            ConfigurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args, mapper)
                .AddJsonFile("appsettings.Development.json", true)
                .Build();

            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServices(services);
                })
                .UseConsoleLifetime();

            ServiceProviderRoot = hostBuilder.Build().Services;
        }

        /// <summary>
        /// 注册容器
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureServices(IServiceCollection services)
        {
            //配置
            services.AddSingleton<IConfiguration>(ConfigurationRoot);

            //Options
            services.AddOptions()
                .Configure<DailyTaskOptions>(ConfigurationRoot.GetSection("DailyTaskConfig"))
                .Configure<JsonSerializerOptions>(o => o = JsonSerializerOptionsBuilder.DefaultOptions)
                .Configure<BiliBiliCookiesOptions>(o => ConfigurationRoot.GetSection("BiliBiliCookies"));

            //日志
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Program.ConfigurationRoot.GetSection("Logging"))
                    .AddConsole()
                    .AddDebug();
            });

            services.AddSingleton<BiliBiliCookiesOptions>(ConfigurationRoot.GetSection("BiliBiliCookies").Get<BiliBiliCookiesOptions>());

            services.AddHttpClient();
            services.AddHttpClient("BiliBiliWithCookies",
                (sp, c) => c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<BiliBiliCookiesOptions>().ToString()));

            //注册强类型api客户端
            services.AddBiliBiliClient<IDailyTaskApi>("https://api.bilibili.com");
            services.AddBiliBiliClient<IMangaApi>("https://manga.bilibili.com");
            services.AddBiliBiliClient<IExperienceApi>("https://www.bilibili.com");
            services.AddBiliBiliClient<IAccountApi>("https://account.bilibili.com");
            services.AddBiliBiliClient<ILiveApi>("https://api.live.bilibili.com");

            services.AddTransient<IDailyTaskAppService, DailyTaskAppService>();
            services.AddTransient<DailyTaskAppService>();
            services.AddDomainServices();
        }
    }
}
