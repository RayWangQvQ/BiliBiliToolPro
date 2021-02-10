using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace LogTest
{
    public class UnitTest1
    {
        public UnitTest1()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void Test1()
        {
            Program.CreateHost(new string[] { });

            var logger = Global.ServiceProviderRoot.GetRequiredService<ILogger<UnitTest1>>();

            logger.LogTrace("testTrace");
            logger.LogDebug("testDebug");
            logger.LogInformation("testInfo");
            logger.LogError("testError");

            logger.LogDebug(null);
            logger.LogDebug("123{0}{1}", null, "haha");

            System.Console.ReadLine();
        }

        [Fact]
        public void Test2()
        {
            ILogger logger = CreateLogger();

            logger.LogInformation("开源地址：{url}", "https://github.com/RayWangQvQ/BiliBiliTool");
            logger.LogInformation("当前环境：{env} \r\n", Global.HostingEnvironment.EnvironmentName ?? "无");

            logger.LogInformation("-----开始每日任务-----\r\n");

            logger.LogInformation("---开始【{taskName}】---", "登录");
            logger.LogInformation("登录成功，经验+5 √");
            logger.LogInformation("用户名: 在*楼");
            logger.LogInformation("---结束---\r\n");

            logger.LogInformation("-----全部任务已执行结束-----\r\n");

            logger.LogInformation("开始推送");
            System.Console.ReadLine();
        }

        private ILogger CreateLogger()
        {
            /*
            return new LoggerConfiguration()
                .WriteTo.Debug()
                .CreateLogger();
            */
            return Global.ServiceProviderRoot.GetRequiredService<ILogger<UnitTest1>>();
        }
    }
}
