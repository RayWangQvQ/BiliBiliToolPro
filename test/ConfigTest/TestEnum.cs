using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Enums;
using Xunit;

namespace ConfigTest
{
    public class TestEnum
    {
        public TestEnum()
        {
            Program.CreateHost(null);
        }

        [Fact]
        public void Test1()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();

            var en = Global.ConfigurationRoot.GetSection("PlatformType").Get<PlatformType>();
        }
    }
}
