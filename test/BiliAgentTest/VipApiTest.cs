using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Xunit.Abstractions;
namespace BiliAgentTest;

public class VipApiTest
{
    private readonly ITestOutputHelper _output;
    public VipApiTest(ITestOutputHelper output)
    {
        _output = output;
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public async Task VipInfoTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();

        var ck = scope.ServiceProvider.GetRequiredService<BiliCookie>();
        var api = scope.ServiceProvider.GetRequiredService<IVipBigPointApi>();
        var re = await api.GetVouchersInfo();
        if (re.Code == 0)
        {
            var info = re.Data.list.Find(x => x.Type == 9);
            if (info != null)
            {
                _output.WriteLine(info.State.ToString());
            }
            else
            {
                _output.WriteLine("error");
            }
        }
    }
}
