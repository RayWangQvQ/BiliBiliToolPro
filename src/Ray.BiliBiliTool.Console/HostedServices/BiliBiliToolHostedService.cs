using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console.HostedServices
{
    public class BiliBiliToolHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BiliBiliToolHostedService> _logger;
        private readonly CookieStrFactory _cookieStrFactory;

        public BiliBiliToolHostedService(
            IHostApplicationLifetime applicationLifetime
            , IServiceProvider serviceProvider
            , IConfiguration configuration
            , ILogger<BiliBiliToolHostedService> logger
            , CookieStrFactory cookieStrFactory
        )
        {
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _cookieStrFactory = cookieStrFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var tasks = _configuration["RunTasks"]
                    .Split("&", options: StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < _cookieStrFactory.Count; i++)
                {
                    _cookieStrFactory.CurrentNum = i + 1;
                    _logger.LogInformation("开始账号 {num} ：" + Environment.NewLine, _cookieStrFactory.CurrentNum);

                    try
                    {
                        DoTasks(tasks);
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

        private void DoTasks(string[] tasks)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var appServices = scope.ServiceProvider.GetRequiredService<IEnumerable<IAppService>>();
                foreach (var task in tasks)
                {
                    var appService = appServices.FirstOrDefault(x => x.TaskName == task);
                    if (appService == null) _logger.LogWarning("任务不存在：{task}", task);
                    appService?.DoTask();
                }
            }
        }
    }
}
