using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace Ray.BiliBiliTool.Agent.FunctionalTests
{
    [Trait("Category", "External")]
    public class LiveTraceApiTests : IDisposable
    {
        private readonly IHost _host;

        public LiveTraceApiTests()
        {
            _host = Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public async Task WebHeartBeat_ValidRoom_ReturnsNextInterval()
        {
            using var scope = _host.Services.CreateScope();

            var ck = ExternalCookie.Require(scope.ServiceProvider);
            var api = scope.ServiceProvider.GetRequiredService<ILiveTraceApi>();

            var request = new WebHeartBeatRequest(63666, 60);

            var re = await api.WebHeartBeat(request, ck.ToString());

            Assert.Equal(0, re.Code);
            Assert.Equal("0", re.Message);
            Assert.Equal(60, re.Data!.Next_interval);
        }

        public void Dispose() => _host.Dispose();
    }
}
