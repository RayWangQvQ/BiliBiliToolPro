using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Console.HostedServices
{
    public class LogAppInfoHostedService : IHostedService
    {
        private readonly ILogger<LogAppInfoHostedService> _logger;

        public LogAppInfoHostedService(
            ILogger<LogAppInfoHostedService> logger
        )
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "【版本号】Ray.BiliBiliTool-v{version}",
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion);
            _logger.LogInformation("【当前IP】{ip} ", IpHelper.GetIp());
            //_logger.LogInformation("当前环境：{env}", Global.HostingEnvironment.EnvironmentName);
            _logger.LogInformation("【开源地址】 {url}" + Environment.NewLine, Constants.SourceCodeUrl);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
