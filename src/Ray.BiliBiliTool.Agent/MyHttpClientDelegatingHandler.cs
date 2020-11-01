using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent
{
    /// <summary>
    /// HttpClient切面
    /// </summary>
    public class MyHttpClientDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<MyHttpClientDelegatingHandler> _logger;

        public MyHttpClientDelegatingHandler(ILogger<MyHttpClientDelegatingHandler> logger)
        {
            this._logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //记录请求内容
            _logger.LogDebug($"[{request.Method}] {request.RequestUri}");

            if (request.Content != null)
            {
                _logger.LogDebug(await request.Content.ReadAsStringAsync());
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            //记录返回内容
            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug(content);

                //如果返回不是json格式，则抛异常
                string msg = "Api返回Content序列化异常，怀疑为不标准返回类型";
                try
                {
                    var ob = JsonSerializer.Deserialize<object>(content);
                    if (ob == null)
                    {
                        _logger.LogCritical(msg);
                        throw new Exception(msg);
                    }
                }
                catch (Exception)
                {
                    _logger.LogInformation(msg);
                    throw;
                }
            }
            return response;
        }
    }
}
