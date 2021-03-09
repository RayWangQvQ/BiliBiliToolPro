using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Console
{
    public class BiliBiliToolHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<BiliBiliToolHostedService> _logger;
        private readonly BiliCookie _biliBiliCookie;
        private readonly IDailyTaskAppService _dailyTaskAppService;
        private readonly ILiveTaskAppService _liveTaskAppService;

        public BiliBiliToolHostedService(
            IHostApplicationLifetime applicationLifetime
            , ILogger<BiliBiliToolHostedService> logger
            , BiliCookie biliBiliCookie
            , IDailyTaskAppService dailyTaskAppService
            , ILiveTaskAppService liveTaskAppService
            )
        {
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _biliBiliCookie = biliBiliCookie;
            _dailyTaskAppService = dailyTaskAppService;
            _liveTaskAppService = liveTaskAppService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var logger = _logger;
            LogAppInfo(logger);

            try
            {
                _dailyTaskAppService.DoDailyTask();
                //_liveTaskAppService.DoLotteryTask();
            }
            catch (Exception ex)
            {
                logger.LogError("程序异常终止，原因：{msg}", ex.Message);
                throw;
            }
            finally
            {
                logger.LogInformation("开始推送");

                if (Global.ConfigurationRoot["CloseConsoleWhenEnd"] == "1")
                {
                    _logger.LogInformation("正在自动关闭应用...");
                    _applicationLifetime.StopApplication();
                }
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
                    ?.InformationalVersion);
            logger.LogInformation("开源地址：{url}", Constants.SourceCodeUrl);
            logger.LogInformation("当前环境：{env}", Global.HostingEnvironment.EnvironmentName);
            logger.LogInformation("当前IP：{ip} \r\n", IpHelper.GetIp());
        }
    }
}
