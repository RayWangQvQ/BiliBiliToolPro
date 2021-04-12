using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Console.HostedServices
{
    public class RandomSleepHostedService : IHostedService
    {
        private readonly ILogger<RandomSleepHostedService> _logger;
        private readonly SecurityOptions _securityOptions;

        public RandomSleepHostedService(
            ILogger<RandomSleepHostedService> logger
            , IOptionsMonitor<SecurityOptions> securityOptions
        )
        {
            _logger = logger;
            _securityOptions = securityOptions.CurrentValue;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_securityOptions.RandomSleepMaxMin > 0)
            {
                int randomMin = new Random().Next(1, ++_securityOptions.RandomSleepMaxMin);
                _logger.LogInformation("随机休眠{min}分钟" + Environment.NewLine, randomMin);
                Thread.Sleep(randomMin * 1000 * 60);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
