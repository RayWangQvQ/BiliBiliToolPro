using System;
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
            PrintLogo();

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

            //hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());

            #region 承载系统自身的配置

            hostBuilder.ConfigureHostConfiguration(hostConfigurationBuilder =>
            {
                hostConfigurationBuilder.AddEnvironmentVariables(prefix: "DOTNET_");

                if (args is { Length: > 0 })
                {
                    hostConfigurationBuilder.AddCommandLine(args);
                }
            });

            #endregion 承载系统自身的配置

            #region 应用配置

            hostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                Global.HostingEnvironment = hostBuilderContext.HostingEnvironment;
                IHostEnvironment env = hostBuilderContext.HostingEnvironment;

                //json文件：
                string envName = hostBuilderContext.HostingEnvironment.EnvironmentName;
                configurationBuilder.AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile($"appsettings.{envName}.json", true, true)
                    ;

                //用户机密：
                if (env.IsDevelopment() && env.ApplicationName?.Length > 0)
                {
                    //var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    Assembly appAssembly = Assembly.GetAssembly(typeof(Program));
                    configurationBuilder.AddUserSecrets(appAssembly, optional: true, reloadOnChange: true);
                }

                //环境变量：
                configurationBuilder.AddExcludeEmptyEnvironmentVariables("QL_", false);
                configurationBuilder.AddExcludeEmptyEnvironmentVariables("Ray_");

                //命令行：
                if (args != null && args.Length > 0)
                {
                    configurationBuilder.AddCommandLine(args, Config.Constants.GetCommandLineMappingsDic());
                }

                //本地cookie存储文件
                configurationBuilder.AddJsonFile("cookies.json", true, true);

                //内置配置
                configurationBuilder.AddInMemoryCollection(Config.Constants.GetExpDic());
                configurationBuilder.AddInMemoryCollection(Config.Constants.GetDonateCoinCanContinueStatusDic());
            });

            #endregion 应用配置

            #region 日志

            hostBuilder.ConfigureLogging((hostBuilderContext, loggingBuilder) =>
            {
                Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostBuilderContext.Configuration)
                .CreateLogger();
                SelfLog.Enable(x => System.Console.WriteLine(x ?? ""));
            })
                .UseSerilog();

            #endregion 日志

            #region DI容器

            hostBuilder.ConfigureServices((hostContext, services) =>
            {
                Global.ConfigurationRoot = (IConfigurationRoot)hostContext.Configuration;

                services.AddHostedService<BiliBiliToolHostedService>();

                services.AddBiliBiliConfigs(hostContext.Configuration);
                services.AddBiliBiliClientApi(hostContext.Configuration);
                services.AddDomainServices();
                services.AddAppServices();
            });

            #endregion DI容器

            return hostBuilder;
        }

        /// <summary>
        /// 输出本工具启动logo
        /// </summary>
        private static void PrintLogo()
        {
            System.Console.WriteLine(@"  ____               ____    _   _____           _  ");
            System.Console.WriteLine(@" |  _ \ __ _ _   _  | __ ) _| |_|_   _|__   ___ | | ");
            System.Console.WriteLine(@" | |_) / _` | | | | |  _ \(_) (_) | |/ _ \ / _ \| | ");
            System.Console.WriteLine(@" |  _ < (_| | |_| | | |_) | | | | | | (_) | (_) | | ");
            System.Console.WriteLine(@" |_| \_\__,_|\__, | |____/|_|_|_| |_|\___/ \___/|_| ");
            System.Console.WriteLine(@"             |___/                                  ");
            System.Console.WriteLine();
        }
    }
}
