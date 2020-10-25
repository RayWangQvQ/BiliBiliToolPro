using System;
using System.Text.Json;
using BiliBiliTool.Agent;
using BiliBiliTool.Agent.Interfaces;
using BiliBiliTool.Config;
using BiliBiliTool.Login;
using BiliBiliTool.Task;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Console.Agent.Interfaces;

namespace BiliBiliTool
{
    public class Program
    {
        public static IConfigurationRoot ConfigurationRoot { get; set; }

        public static IServiceProvider ServiceProviderRoot { get; set; }


        static void Main(string[] args)
        {
            PreWorks(new Verify(args[0], args[1], args[2]));

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
                logger.LogDebug("-----任务启动-----");
                DailyTask dailyTask = serviceScope.ServiceProvider.GetRequiredService<DailyTask>();
                dailyTask.DoDailyTask();
            }
            Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="verify"></param>
        public static void PreWorks(Verify verify)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    //配置
                    ConfigurationRoot = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();
                    services.AddSingleton<IConfiguration>(ConfigurationRoot);

                    //Options
                    services.AddOptions()
                        .Configure<DailyTaskOptions>(ConfigurationRoot.GetSection("DailyTaskConfig"))
                        .Configure<JsonSerializerOptions>(o => o = JsonSerializerOptionsBuilder.DefaultOptions);

                    //日志
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole()
                            .AddDebug()
                            .SetMinimumLevel(LogLevel.Information);
                    });

                    services.AddSingleton(verify);

                    services.AddHttpClient();
                    services.AddHttpClient("BiliBiliWithCookies",
                        (sp, c) => c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<Verify>().getVerify()));
                    //注册强类型api客户端
                    services.AddBiliBiliClient<IDailyTaskApi>("https://api.bilibili.com");
                    services.AddBiliBiliClient<IMangaApi>("https://manga.bilibili.com");
                    services.AddBiliBiliClient<IExperienceApi>("https://www.bilibili.com");
                    services.AddBiliBiliClient<IAccountApi>("https://account.bilibili.com");
                    services.AddBiliBiliClient<ILiveApi>("https://api.live.bilibili.com");

                    services.AddSingleton<LoginResponse>();
                    services.AddTransient<DailyTask>();
                })
                .UseConsoleLifetime();

            ServiceProviderRoot = hostBuilder.Build().Services;
        }
    }
}
