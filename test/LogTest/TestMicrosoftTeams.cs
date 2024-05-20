using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.CoolPushBatched;
using Ray.Serilog.Sinks.MicrosoftTeamsBatched;
using Ray.Serilog.Sinks.PushPlusBatched;
using Ray.Serilog.Sinks.ServerChanBatched;
using Xunit;

namespace LogTest
{
    public class TestMicrosoftTeams
    {
        private string _webhook;

        public TestMicrosoftTeams()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { "ENVIRONMENT=Development" });

            _webhook = Global.ConfigurationRoot["Serilog:WriteTo:10:Args:webhook"];
        }

        [Fact]
        public void Test()
        {
            var client = new MicrosoftTeamsApiClient(webhook: _webhook);

            var msg = LogConstants.Msg2;

            var result = client.PushMessage(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            Assert.True(result.StatusCode == System.Net.HttpStatusCode.OK);
        }
    }
}
