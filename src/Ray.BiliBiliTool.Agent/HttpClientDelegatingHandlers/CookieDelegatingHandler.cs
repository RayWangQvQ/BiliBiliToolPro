using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers
{
    public class CookieDelegatingHandler : HttpClientHandler
    {
        private readonly ILogger<CookieDelegatingHandler> _logger;
        private readonly BiliCookieContainer _biliCookieContainer;

        public CookieDelegatingHandler(ILogger<CookieDelegatingHandler> logger, BiliCookieContainer biliCookieContainer)
        {
            _logger = logger;
            _biliCookieContainer = biliCookieContainer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 发送请求之前处理 Cookie
            var cookies = _biliCookieContainer.GetCookieStr();
            // 将 Cookie 设置为请求头
            request.Headers.Add("Cookie", cookies.ToString());

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            // 收到响应后处理 Set-Cookie 头
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var setCookieHeader in setCookieHeaders)
                {
                    this.CookieContainer.SetCookies(response.RequestMessage.RequestUri, setCookieHeader);
                }
            }

            return response;
        }
    }
}
