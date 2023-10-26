using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class UnfollowBatchedTaskAppService : MultiAccountsAppService, IUnfollowBatchedTaskAppService
    {
        private readonly IAccountDomainService _accountDomainService;

        public UnfollowBatchedTaskAppService(
            ILogger<UnfollowBatchedTaskAppService> logger,
            BiliCookieContainer biliCookieContainer,
            IAccountDomainService accountDomainService
            ):base(logger, biliCookieContainer, accountDomainService)
        {
            _accountDomainService = accountDomainService;
        }

        protected override async Task DoEachAccountAsync(CancellationToken cancellationToken)
        {
            await _accountDomainService.UnfollowBatched();
        }
    }
}
