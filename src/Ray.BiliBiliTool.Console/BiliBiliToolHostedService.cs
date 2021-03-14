using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Console
{
    public class BiliBiliToolHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BiliBiliToolHostedService> _logger;
        private readonly BiliCookie _biliBiliCookie;
        private readonly IEnumerable<IAppService> _appServices;
        private readonly SecurityOptions _securityOptions;

        public BiliBiliToolHostedService(
            IHostApplicationLifetime applicationLifetime
            , IConfiguration configuration
            , ILogger<BiliBiliToolHostedService> logger
            , BiliCookie biliBiliCookie
            , IEnumerable<IAppService> appServices
            , IOptionsMonitor<SecurityOptions> securityOptions
            )
        {
            _applicationLifetime = applicationLifetime;
            _configuration = configuration;
            _logger = logger;
            _biliBiliCookie = biliBiliCookie;
            _appServices = appServices.ToList();
            _securityOptions = securityOptions.CurrentValue;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            LogAppInfo();

            try
            {
                var tasks = _configuration["RunTasks"]
                    .Split("&", options: StringSplitOptions.RemoveEmptyEntries);
                if (!tasks.Any()) return Task.CompletedTask;

                if (CheckSkip()) return Task.CompletedTask;

                RandomSleep();

                foreach (var task in tasks)
                {
                    var appService = _appServices.FirstOrDefault(x => x.TaskName == task);
                    if (appService == null) _logger.LogWarning("任务不存在：{task}", task);
                    appService?.DoTask();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("程序异常终止，原因：{msg}", ex.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("开始推送");

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
        private void LogAppInfo()
        {
            _logger.LogInformation(
                "版本号：Ray.BiliBiliTool-v{version}",
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion);
            _logger.LogInformation("开源地址：{url}", Constants.SourceCodeUrl);
            _logger.LogInformation("当前环境：{env}", Global.HostingEnvironment.EnvironmentName);
            _logger.LogInformation("当前IP：{ip} \r\n", IpHelper.GetIp());
        }

        public bool CheckSkip()
        {
            if (_securityOptions.IsSkipDailyTask)
            {
                _logger.LogWarning("已配置为跳过任务\r\n");
                return true;
            }

            return false;
        }

        public void RandomSleep()
        {
            if (_securityOptions.RandomSleepMaxMin > 0)
            {
                int randomMin = new Random().Next(1, ++_securityOptions.RandomSleepMaxMin);
                _logger.LogInformation("随机休眠{min}分钟 \r\n", randomMin);
                Thread.Sleep(randomMin * 1000 * 60);
            }
        }
    }
}
