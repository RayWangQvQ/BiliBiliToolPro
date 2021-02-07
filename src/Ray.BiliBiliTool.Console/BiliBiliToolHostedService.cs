using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console
{
    public class BiliBiliToolHostedService : IHostedService
    {
        private readonly ILogger<BiliBiliToolHostedService> _logger;
        private readonly BiliCookie _biliBiliCookie;
        private readonly IDailyTaskAppService _dailyTaskAppService;

        public BiliBiliToolHostedService(
            ILogger<BiliBiliToolHostedService> logger
            , BiliCookie biliBiliCookie
            , IDailyTaskAppService dailyTaskAppService
            )
        {
            _logger = logger;
            _biliBiliCookie = biliBiliCookie;
            _dailyTaskAppService = dailyTaskAppService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var logger = _logger;
            LogAppInfo(logger);

            try
            {
                BiliCookie biliBiliCookie = _biliBiliCookie;

                IDailyTaskAppService dailyTask = _dailyTaskAppService;

                dailyTask.DoDailyTask();
            }
            catch (Exception ex)
            {
                logger.LogError("程序异常终止，原因：{msg}", ex.Message);
                throw;
                //Environment.Exit(1);
            }
            finally
            {
                logger.LogInformation("开始推送");

                //如果配置了“1”就立即关闭，否则保持窗口以便查看日志信息
                if (Global.ConfigurationRoot["CloseConsoleWhenEnd"] == "1") Environment.Exit(1);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 打印应用信息
        /// </summary>
        /// <param name="logger"></param>
        private static void LogAppInfo(Microsoft.Extensions.Logging.ILogger logger)
        {
            logger.LogInformation(
                "版本号：Ray.BiliBiliTool-v{version}",
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "未知");
            logger.LogInformation("开源地址：{url}", Constants.SourceCodeUrl);
            logger.LogInformation("当前环境：{env}", Global.HostingEnvironment.EnvironmentName ?? "无");
            try
            {
                logger.LogInformation("当前IP：{ip} \r\n", new HttpClient().GetAsync("http://api.ipify.org/").Result.Content.ReadAsStringAsync().Result);
            }
            catch (Exception)
            {
                //Environment.Exit(1);
            }
        }
    }
}
