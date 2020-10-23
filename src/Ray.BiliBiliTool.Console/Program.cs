using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Unicode;
using BiliBiliTool.Agent.Interfaces;
using BiliBiliTool.Config;
using BiliBiliTool.Login;
using BiliBiliTool.Task;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Refit;

namespace BiliBiliTool
{
    public class Program
    {
        public static IConfigurationRoot ConfigurationRoot { get; set; }

        public static IServiceProvider ServiceProviderRoot { get; set; }

        private static string PC_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36 Edg/85.0.564.70";

        static void Main(string[] args)
        {
            PreWorks(new Verify(args[0], args[1], args[2]));

            ILogger logger = ServiceProviderRoot.GetRequiredService<ILogger<Program>>();

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
            DailyTask dailyTask = ServiceProviderRoot.GetRequiredService<DailyTask>();
            dailyTask.DoDailyTask();

            Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="verify"></param>
        public static void PreWorks(Verify verify)
        {
            //全局设置默认的序列化配置：驼峰式、支持中文（目前System.Text.Json不支持设置默认Options，这里用反射实现了，以后.net5中可能会新增默认options的接口）（https://github.com/dotnet/runtime/issues/31094）
            JsonSerializerOptions defaultJsonSerializerOptions = (JsonSerializerOptions)typeof(JsonSerializerOptions)
                .GetField("s_defaultOptions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null);
            defaultJsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            defaultJsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All);

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
                        .Configure<DailyTaskOptions>(ConfigurationRoot.GetSection("DailyTaskConfig"));

                    services.AddLogging(builder =>
                    {
                        builder.AddConsole().SetMinimumLevel(LogLevel.Information);
                        builder.AddDebug().SetMinimumLevel(LogLevel.Information);
                    });

                    services.AddSingleton(verify);

                    var settings = new RefitSettings(new SystemTextJsonContentSerializer(defaultJsonSerializerOptions));
                    services.AddRefitClient<IDailyTaskApi>(settings)
                        .ConfigureHttpClient(
                            (sp, c) =>
                            {
                                SetBiliDefaultRequestHeaders(sp, c);
                                c.BaseAddress = new Uri("https://api.bilibili.com");
                            });
                    services.AddRefitClient<IMangaApi>(settings)
                        .ConfigureHttpClient(
                            (sp, c) =>
                            {
                                SetBiliDefaultRequestHeaders(sp, c);
                                c.BaseAddress = new Uri("https://manga.bilibili.com");
                            });

                    services.AddTransient<DailyTask>();
                })
                .UseConsoleLifetime();

            ServiceProviderRoot = hostBuilder.Build().Services;
        }

        /// <summary>
        /// 设置请求的默认headers
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="c"></param>
        private static void SetBiliDefaultRequestHeaders(IServiceProvider sp, HttpClient c)
        {
            c.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            c.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com/");
            c.DefaultRequestHeaders.Add("Connection", "keep-alive");
            c.DefaultRequestHeaders.Add("User-Agent", PC_USER_AGENT);
            c.DefaultRequestHeaders.Add("Cookie", sp.GetRequiredService<Verify>().getVerify());
        }
    }
}
