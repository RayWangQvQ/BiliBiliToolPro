using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console
{
    public class BiliBiliToolHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BiliBiliToolHostedService> _logger;
        private readonly CookieStrFactory _cookieStrFactory;
        private readonly SecurityOptions _securityOptions;

        public BiliBiliToolHostedService(
            IHostApplicationLifetime applicationLifetime
            , IServiceProvider serviceProvider
            , IConfiguration configuration
            , ILogger<BiliBiliToolHostedService> logger
            , CookieStrFactory cookieStrFactory
            , IOptionsMonitor<SecurityOptions> securityOptions)
        {
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _cookieStrFactory = cookieStrFactory;
            _securityOptions = securityOptions.CurrentValue;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var isNotifySingle = _configuration.GetSection("Notification:IsSingleAccountSingleNotify").Get<bool>();

            try
            {
                _logger.LogInformation("BiliBiliToolPro 开始运行...{newLine}", Environment.NewLine);

                var pass = PreCheck();
                if (!pass) return Task.CompletedTask;

                RandomSleep();

                var tasks = _configuration["RunTasks"]
                    .Split("&", options: StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < _cookieStrFactory.Count; i++)
                {
                    _cookieStrFactory.CurrentNum = i + 1;
                    _logger.LogInformation("######### 账号 {num} #########{newLine}", _cookieStrFactory.CurrentNum, Environment.NewLine);

                    try
                    {
                        DoTasks(tasks);
                        if (isNotifySingle)
                        {
                            LogAppInfo();

                            var accountName = _cookieStrFactory.Count > 1 ? $"账号【{_cookieStrFactory.CurrentNum}】" : "";
                            _logger.LogInformation("·开始推送·{task}·{user}", $"{_configuration["RunTasks"]}任务", accountName);
                        }
                    }
                    catch (Exception e)
                    {
                        //ignore
                        _logger.LogWarning("异常：{msg}", e);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("程序异常终止，原因：{msg}", ex.Message);
                throw;
            }
            finally
            {
                if (!isNotifySingle)
                {
                    LogAppInfo();
                    _logger.LogInformation("·开始推送·{task}·{user}", $"{_configuration["RunTasks"]}任务", "");
                }
                _logger.LogInformation("运行结束");
                _applicationLifetime.StopApplication();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private bool PreCheck()
        {
            //目标任务
            _logger.LogInformation("【目标任务】{tasks}", _configuration["RunTasks"]);
            var tasks = _configuration["RunTasks"]
                .Split("&", options: StringSplitOptions.RemoveEmptyEntries);
            if (!tasks.Any()) return false;

            //Cookie
            _logger.LogInformation("【账号个数】{count}个{newLine}", _cookieStrFactory.Count, Environment.NewLine);
            if (_cookieStrFactory.Count == 0) return false;

            //是否跳过
            if (_securityOptions.IsSkipDailyTask)
            {
                _logger.LogWarning("已配置为跳过任务{newLine}", Environment.NewLine);
                return false;
            }

            return true;
        }

        private Task RandomSleep()
        {
            if (_securityOptions.RandomSleepMaxMin > 0)
            {
                int randomMin = new Random().Next(1, ++_securityOptions.RandomSleepMaxMin);
                _logger.LogInformation("随机休眠{min}分钟{newLine}", randomMin, Environment.NewLine);
                Thread.Sleep(randomMin * 1000 * 60);
            }

            return Task.CompletedTask;
        }

        private void DoTasks(string[] tasks)
        {
            using var scope = _serviceProvider.CreateScope();
            foreach (var task in tasks)
            {
                var type = TaskTypeFactory.Create(task);
                if (type == null)
                {
                    _logger.LogWarning("任务不存在：{task}", task);
                    continue;
                }

                var appService = (IAppService)scope.ServiceProvider.GetRequiredService(type);
                appService?.DoTask();
            }
        }

        private void LogAppInfo()
        {
            _logger.LogInformation(
                "{newLine}========================{newLine} v{version} in {env} env.{newLine}开源 by {url}",
                Environment.NewLine + Environment.NewLine,
                Environment.NewLine + Environment.NewLine,
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                Global.HostingEnvironment.EnvironmentName,
                Environment.NewLine,
                Constants.SourceCodeUrl + Environment.NewLine
                );
            //_logger.LogInformation("【当前IP】{ip} ", IpHelper.GetIp());
        }
    }
}
