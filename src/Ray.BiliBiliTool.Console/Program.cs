using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console.Helpers;
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
            if (RayConfiguration.Root["CloseConsoleWhenEnd"] == "1") return;
            else System.Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="args"></param>
        public static void PreWorks(string[] args)
        {
            //配置:
            RayConfiguration.Root = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFileByEnv()
                .AddExcludeEmptyEnvironmentVariables("Ray_")
                .AddCommandLine(args, Constants.CommandLineMapper)
                .Build();

            //日志:
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(RayConfiguration.Root)
                .WriteTo.TextWriter(
                    textWriter: PushService.PushStringWriter,
                    restrictedToMinimumLevel: LogHelper.GetConsoleLogLevel(),
                    outputTemplate: LogHelper.GetConsoleLogTemplate() + "\r\n")//用来做微信推送
                .CreateLogger();

            //Host:
            IHostBuilder hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConfiguration>(RayConfiguration.Root);
                    services.AddBiliBiliConfigs(RayConfiguration.Root);
                    services.AddBiliBiliClientApi();
                    services.AddDomainServices();
                    services.AddAppServices();
                })
                .UseSerilog()
                .UseConsoleLifetime();

            RayContainer.Root = hostBuilder.Build().Services;
        }

        /// <summary>
        /// 开始运行
        /// </summary>
        public static void StartRun()
        {
            using IServiceScope serviceScope = RayContainer.Root.CreateScope();

            //初始化DI相关的部分
            IServiceProvider di = serviceScope.ServiceProvider;
            RayContainer.SetGetServiceFunc(type => di.GetService(type));

            ILogger<Program> logger = di.GetRequiredService<ILogger<Program>>();

            logger.LogInformation(
                "版本号：{version}",
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "未知");
            logger.LogInformation("开源地址：{url} \r\n", Constants.SourceCodeUrl);
            logger.LogInformation("当前环境：{env} \r\n", RayConfiguration.Env ?? "无");

            BiliBiliCookieOptions biliBiliCookieOptions = di.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue;
            if (!biliBiliCookieOptions.Check(logger))
                throw new Exception($"请正确配置Cookie后再运行，配置方式见 {Constants.SourceCodeUrl}");

            IDailyTaskAppService dailyTask = di.GetRequiredService<IDailyTaskAppService>();
            PushService pushService = di.GetRequiredService<PushService>();

            try
            {
                dailyTask.DoDailyTask();
            }
            catch
            {
                pushService.SendStringWriter();
                throw;
            }

            pushService.SendStringWriter();
        }
    }
}
