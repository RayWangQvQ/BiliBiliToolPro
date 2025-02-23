using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application;

public class TestAppService(
    IConfiguration configuration,
    ILogger<LiveLotteryTaskAppService> logger,
    IAccountDomainService accountDomainService)
    : AppService, ITestAppService
{
    [TaskInterceptor("测试Cookie")]
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        await accountDomainService.LoginByCookie();
    }
}