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
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Infrastructure.Enums;

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

            var en = Global.ConfigurationRoot.GetSection("PlateformType").Get<PlateformType>();
        }
    }
}
