using System;
using BiliBiliTool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new BiliBiliTool.Login.Verify("", "", ""));

            var logger = Program.ServiceProviderRoot.GetRequiredService<ILogger<UnitTest1>>();

            logger.LogInformation("testInfo");
            logger.LogDebug("testDebug");
        }
    }
}
