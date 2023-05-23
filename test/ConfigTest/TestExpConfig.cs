using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Console;
using Xunit;
using Ray.BiliBiliTool.Infrastructure;
using System.Collections.Generic;

namespace ConfigTest
{
    public class TestExpConfig
    {
        public TestExpConfig()
        {
            Program.CreateHost(null);
        }

        [Fact]
        public void Test1()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<Dictionary<string, int>>>();
            var dic = options.Get(Constants.OptionsNames.ExpDictionaryName);

            Debug.WriteLine(dic.ToJsonStr());

            Assert.True(dic.Count > 0);
        }
    }
}
