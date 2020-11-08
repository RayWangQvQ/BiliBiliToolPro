using System;
using System.IO;
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
            System.Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="args"></param>
        public static void PreWorks(string[] args)
        {
            RayConfiguration.Root = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddCommandLine(args, Constants.CommandLineMapper)
                //.AddJsonFile("appsettings.local.json", true,true)
                .Build();

            //日志:
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(RayConfiguration.Root)
                .WriteTo.TextWriter(PushService.PushStringWriter,
                                    Serilog.Events.LogEventLevel.Information,
                                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}\r\n")//用来做微信推送
                .CreateLogger();

            //Host:
            var hostBuilder = new HostBuilder()
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
            using (var serviceScope = RayContainer.Root.CreateScope())
            {
                var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                BiliBiliCookieOptions biliBiliCookieOptions = serviceScope.ServiceProvider.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>().CurrentValue;
                if (!biliBiliCookieOptions.Check(logger))
                    throw new Exception("请正确配置Cookie后再运行，配置方式见 https://github.com/RayWangQvQ/BiliBiliTool");

                IDailyTaskAppService dailyTask = serviceScope.ServiceProvider.GetRequiredService<IDailyTaskAppService>();
                var pushService = serviceScope.ServiceProvider.GetRequiredService<PushService>();
                bool isPushed = false;

                try
                {
                    dailyTask.DoDailyTask();
                }
                catch (Exception ex)
                {
                    pushService.SendStringWriter();
                    isPushed = true;
                }

                if (!isPushed) pushService.SendStringWriter();
            }
        }
    }
}
