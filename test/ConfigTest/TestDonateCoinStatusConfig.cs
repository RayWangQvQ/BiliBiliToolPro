using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace ConfigTest
{
    public class TestDonateCoinStatusConfig
    {
        public TestDonateCoinStatusConfig()
        {
            Program.CreateHost(null);
        }

        [Fact]
        public void Test1()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<
                IOptionsMonitor<Dictionary<string, string>>
            >();
            var dic = options.Get(Constants.OptionsNames.DonateCoinCanContinueStatusDictionaryName);

            Debug.WriteLine(dic.ToJsonStr());

            Assert.True(dic.Count > 0);
        }
    }
}
