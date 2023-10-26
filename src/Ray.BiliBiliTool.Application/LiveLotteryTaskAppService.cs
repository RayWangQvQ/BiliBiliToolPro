using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class LiveLotteryTaskAppService : MultiAccountsAppService, ILiveLotteryTaskAppService
    {
        private readonly ILogger<LiveLotteryTaskAppService> _logger;
        private readonly ILiveDomainService _liveDomainService;
        private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions;
        private readonly IAccountDomainService _accountDomainService;

        public LiveLotteryTaskAppService(
            ILogger<LiveLotteryTaskAppService> logger,
            BiliCookieContainer biliCookieContainer,
            IAccountDomainService accountDomainService,
            ILiveDomainService liveDomainService,
                IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions
            ):base(logger, biliCookieContainer, accountDomainService)
        {
            _liveDomainService = liveDomainService;
            _liveLotteryTaskOptions = liveLotteryTaskOptions.CurrentValue;
            _logger = logger;
            _accountDomainService = accountDomainService;
        }

        protected override async Task DoEachAccountAsync(CancellationToken cancellationToken)
        {
            await LogUserInfo();
            await LotteryTianXuan();
            await AutoGroupFollowings();
        }

        [TaskInterceptor("打印用户信息")]
        private async Task LogUserInfo()
        {
            await _accountDomainService.LoginByCookie();
        }

        [TaskInterceptor("抽奖")]
        private async Task LotteryTianXuan()
        {
            await _liveDomainService.TianXuan();
        }

        [TaskInterceptor("自动分组关注的主播")]
        private async Task AutoGroupFollowings()
        {
            if (_liveLotteryTaskOptions.AutoGroupFollowings)
            {
                await _liveDomainService.GroupFollowing();
            }
            else
            {
                _logger.LogInformation("配置未开启，跳过");
            }
        }
    }
}
