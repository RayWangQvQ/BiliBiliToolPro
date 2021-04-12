using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Console.HostedServices
{
    public class PreCheckHostedService : IHostedService
    {
        private readonly ILogger<PreCheckHostedService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CookieStrFactory _cookieStrFactory;
        private readonly SecurityOptions _securityOptions;

        public PreCheckHostedService(
            ILogger<PreCheckHostedService> logger
            , IConfiguration configuration
            , IOptionsMonitor<SecurityOptions> securityOptions
            , CookieStrFactory cookieStrFactory
        )
        {
            _logger = logger;
            _configuration = configuration;
            _securityOptions = securityOptions.CurrentValue;
            _cookieStrFactory = cookieStrFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //任务
            var tasks = _configuration["RunTasks"]
                .Split("&", options: StringSplitOptions.RemoveEmptyEntries);
            _logger.LogInformation("【任务】{tasks}", _configuration["RunTasks"]);
            if (!tasks.Any()) return Task.CompletedTask;

            //Cookie
            _logger.LogInformation("【账号】{count}个" + Environment.NewLine, _cookieStrFactory.Count);
            if (_cookieStrFactory.Count == 0) return Task.CompletedTask;

            //是否跳过
            if (CheckSkip()) return Task.CompletedTask;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public bool CheckSkip()
        {
            if (_securityOptions.IsSkipDailyTask)
            {
                _logger.LogWarning("已配置为跳过任务" + Environment.NewLine);
                return true;
            }

            return false;
        }
    }
}
