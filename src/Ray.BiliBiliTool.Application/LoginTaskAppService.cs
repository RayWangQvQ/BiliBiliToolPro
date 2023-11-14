using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure.Enums;
using System.Threading;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class LoginTaskAppService : AppService, ILoginTaskAppService
    {
        private readonly ILogger<LoginTaskAppService> _logger;
        private readonly ILoginDomainService _loginDomainService;
        private readonly IConfiguration _configuration;

        public LoginTaskAppService(
            IConfiguration configuration,
            ILogger<LoginTaskAppService> logger,
            ILoginDomainService loginDomainService)
        {
            _configuration = configuration;
            _logger = logger;
            _loginDomainService = loginDomainService;
        }

        [TaskInterceptor("扫码登录", TaskLevel.One)]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            //扫码登录
            var cookieInfo = await QrCodeLoginAsync(cancellationToken);
            if (cookieInfo == null) return;

            //set cookie
            cookieInfo = await SetCookiesAsync(cookieInfo, cancellationToken);

            //持久化cookie
            await SaveCookieAsync(cookieInfo, cancellationToken);
        }

        [TaskInterceptor("获取二维码", TaskLevel.Two)]
        protected async Task<BiliCookieContainer> QrCodeLoginAsync(CancellationToken cancellationToken)
        {
            var biliCookie = await _loginDomainService.LoginByQrCodeAsync(cancellationToken);
            return biliCookie;
        }

        [TaskInterceptor("Set Cookie", TaskLevel.Two)]
        protected async Task<BiliCookieContainer> SetCookiesAsync(BiliCookieContainer biliCookieContainer, CancellationToken cancellationToken)
        {
            var ck= await _loginDomainService.SetCookieAsync(biliCookieContainer, cancellationToken);
            return ck;
        }

        [TaskInterceptor("持久化Cookie", TaskLevel.Two)]
        protected async Task SaveCookieAsync(BiliCookieContainer ckInfo, CancellationToken cancellationToken)
        {
            var platformType = _configuration.GetSection("PlateformType").Get<PlatformType>();
            _logger.LogInformation("当前运行平台：{platform}",platformType);

            //更新cookie到青龙env
            if (platformType == PlatformType.QingLong)
            {
                await _loginDomainService.SaveCookieToQinLongAsync(ckInfo, cancellationToken);
                return;
            }

            //更新cookie到json
            await _loginDomainService.SaveCookieToJsonFileAsync(ckInfo, cancellationToken);
        }
    }
}
