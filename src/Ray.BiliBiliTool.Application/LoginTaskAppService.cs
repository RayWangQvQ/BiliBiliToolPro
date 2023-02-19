using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure.Enums;
using System.Threading;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

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
        protected async Task<BiliCookie> QrCodeLoginAsync(CancellationToken cancellationToken)
        {
            var biliCookie = await _loginDomainService.LoginByQrCodeAsync(cancellationToken);
            return biliCookie;
        }

        [TaskInterceptor("Set Cookie", TaskLevel.Two)]
        protected async Task<BiliCookie> SetCookiesAsync(BiliCookie biliCookie, CancellationToken cancellationToken)
        {
            var ck= await _loginDomainService.SetCookieAsync(biliCookie, cancellationToken);
            return ck;
        }

        [TaskInterceptor("持久化Cookie", TaskLevel.Two)]
        protected async Task SaveCookieAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
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
