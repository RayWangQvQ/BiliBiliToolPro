using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.MicrosoftTeamsBatched;
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
        public async Task Test()
        {
            var client = new MicrosoftTeamsApiClient(webhook: _webhook);

            var msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            Assert.True(result.StatusCode == System.Net.HttpStatusCode.OK);
        }
    }
}
