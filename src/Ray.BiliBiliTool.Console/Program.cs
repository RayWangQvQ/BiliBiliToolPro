using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Serilog;
using Serilog.Debugging;

namespace Ray.BiliBiliTool.Console
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            IHost host = CreateHost(args);

            try
            {
                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        public static IHost CreateHost(string[] args)
        {
            IHost host = CreateHostBuilder(args)
                .UseConsoleLifetime()
                .Build();
            Global.ServiceProviderRoot = host.Services;
            return host;
        }

        internal static IHostBuilder CreateHostBuilder(string[] args)
        {
            //IHostBuilder hostBuilder = Host.CreateDefaultBuilder();
            IHostBuilder hostBuilder = new HostBuilder();

            hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());

            //承载系统自身的配置：
            hostBuilder.ConfigureHostConfiguration(hostConfigurationBuilder =>
            {
                hostConfigurationBuilder.AddEnvironmentVariables(prefix: "DOTNET_");
            });

            //应用配置:
            hostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                Global.HostingEnvironment = hostBuilderContext.HostingEnvironment;
                IHostEnvironment env = hostBuilderContext.HostingEnvironment;

                //json文件：
                configurationBuilder.AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{hostBuilderContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddJsonFile("exp.json", false, true) //todo：不要使用配置
                    .AddJsonFile("donateCoinCanContinueStatus.json", false, true);//todo：不要使用配置

                //用户机密：
                if (env.IsDevelopment() && env.ApplicationName?.Length > 0)
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    configurationBuilder.AddUserSecrets(appAssembly, optional: true, reloadOnChange: true);
                }

                //环境变量：
                configurationBuilder.AddExcludeEmptyEnvironmentVariables("QL_", false);
                configurationBuilder.AddExcludeEmptyEnvironmentVariables("Ray_");

                //命令行：
                if (args != null && args.Length > 0)
                {
                    configurationBuilder.AddCommandLine(args, hostBuilderContext.Configuration
                        .GetSection("CommandLineMappings")
                        .Get<Dictionary<string, string>>());//todo：不要使用配置
                }

                //本地cookie存储文件
                configurationBuilder.AddJsonFile("cookies.json", true, true);
            });

            //日志:
            hostBuilder.ConfigureLogging((hostBuilderContext, loggingBuilder) =>
            {
                Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostBuilderContext.Configuration)
                .CreateLogger();
                SelfLog.Enable(x => System.Console.WriteLine(x ?? ""));
            }).UseSerilog();

            //DI容器:
            hostBuilder.ConfigureServices((hostContext, services) =>
            {
                Global.ConfigurationRoot = (IConfigurationRoot)hostContext.Configuration;

                services.AddHostedService<BiliBiliToolHostedService>();

                services.AddBiliBiliConfigs(hostContext.Configuration);
                services.AddBiliBiliClientApi(hostContext.Configuration);
                services.AddDomainServices();
                services.AddAppServices();
            });

            return hostBuilder;
        }
    }
}
