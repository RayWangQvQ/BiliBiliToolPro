using System.Threading;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application;

public class UnfollowBatchedTaskAppService(IAccountDomainService accountDomainService)
    : AppService,
        IUnfollowBatchedTaskAppService
{
    [TaskInterceptor("批量取关", TaskLevel.One)]
    public override async Task DoTaskAsync(CancellationToken cancellationToken = default)
    {
        await accountDomainService.UnfollowBatched();
    }
}
