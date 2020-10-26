using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent
{
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
                try
                {
                    var ob = JsonSerializer.Deserialize<object>(content);
                    if (ob == null)
                        throw new NullReferenceException();
                }
                catch (Exception)
                {
                    _logger.LogInformation("接口返回异常");
                    throw;
                }
            }
            return response;
        }
    }
}
