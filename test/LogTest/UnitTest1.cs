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
        }

        [Fact]
        public void Test2()
        {
            ILogger logger = CreateLogger();

            logger.LogInformation("��Դ��ַ��{url}", "https://github.com/RayWangQvQ/BiliBiliTool");
            logger.LogInformation(
                "��ǰ������{env} \r\n",
                Global.HostingEnvironment.EnvironmentName ?? "��"
            );

            logger.LogInformation("-----��ʼÿ������-----\r\n");

            logger.LogInformation("---��ʼ��{taskName}��---", "��¼");
            logger.LogInformation("��¼�ɹ�������+5 ��");
            logger.LogInformation("�û���: ��*¥");
            logger.LogInformation("---����---\r\n");

            logger.LogInformation("-----ȫ��������ִ�н���-----\r\n");

            logger.LogInformation("��ʼ����");
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
