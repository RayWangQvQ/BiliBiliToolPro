using System;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Xunit;
using Ray.BiliBiliTool.Infrastructure;
using Microsoft.Extensions.Hosting;
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

            var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<Dictionary<string, string>>>();
            var dic = options.Get("exp");

            Debug.WriteLine(dic.ToJson());
        }
    }
}
