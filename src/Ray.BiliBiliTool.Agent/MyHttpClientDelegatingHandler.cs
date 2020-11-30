using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Agent
{
    /// <summary>
    /// HttpClient切面
    /// </summary>
    public class MyHttpClientDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<MyHttpClientDelegatingHandler> _logger;
        private readonly SecurityOptions _securityOptions;

        public MyHttpClientDelegatingHandler(ILogger<MyHttpClientDelegatingHandler> logger, IOptionsMonitor<SecurityOptions> securityOptions)
        {
            _logger = logger;
            _securityOptions = securityOptions.CurrentValue;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //记录请求内容
            _logger.LogDebug("发起请求：[{method}] {uri}", request.Method, request.RequestUri);

            if (request.Content != null)
            {
                var requestContent = await request.Content.ReadAsStringAsync();
                _logger.LogDebug("请求Content： {content}", requestContent);
            }

            IntervalForSecurity(request.Method);
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            //记录返回内容
            if (response.Content == null) return response;

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("返回Content：{content}", content);

            //如果返回不是json格式，则抛异常
            string msg = "Api返回Content序列化异常，怀疑为不标准返回类型";
            try
            {
                var ob = JsonSerializer.Deserialize<object>(content);
                if (ob == null)
                {
                    _logger.LogError(msg);
                    throw new Exception(msg);
                }
            }
            catch (Exception)
            {
                _logger.LogInformation(msg);
                throw;
            }
            return response;
        }

        /// <summary>
        /// 安全间隔
        /// </summary>
        /// <param name="method"></param>
        private void IntervalForSecurity(HttpMethod method)
        {
            if (_securityOptions.IntervalSecondsBetweenRequestApi <= 0) return;
            if (!_securityOptions.GetIntervalMethods().Contains(method)) return;

            Task.Delay(_securityOptions.IntervalSecondsBetweenRequestApi * 1000).Wait();
        }
    }
}
