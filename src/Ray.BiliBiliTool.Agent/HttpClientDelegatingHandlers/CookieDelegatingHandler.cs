using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers
{
    public class CookieDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<CookieDelegatingHandler> _logger;
        private readonly BiliCookie _biliCookie;

        public CookieDelegatingHandler(ILogger<CookieDelegatingHandler> logger, BiliCookie biliCookie)
        {
            _logger = logger;
            _biliCookie = biliCookie;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //记录请求内容
            _logger.LogDebug("发起请求：[{method}] {uri}", request.Method, request.RequestUri);

            if (request.Content != null)
            {
                var requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("请求Content： {content}", requestContent);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("返回Content：{content}", content);

            return response;
        }
    }
}
