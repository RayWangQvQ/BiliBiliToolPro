using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class UnfollowBatchedTaskAppService : AppService, IUnfollowBatchedTaskAppService
    {
        private readonly IAccountDomainService _accountDomainService;

        public UnfollowBatchedTaskAppService(
            IAccountDomainService accountDomainService
            )
        {
            _accountDomainService = accountDomainService;
        }

        [TaskInterceptor("批量取关", TaskLevel.One)]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            await _accountDomainService.UnfollowBatched();
        }
    }
}
