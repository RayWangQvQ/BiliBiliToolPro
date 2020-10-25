using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Console
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
                _logger.LogInformation(await request.Content.ReadAsStringAsync());
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            //记录返回内容
            if (response.Content != null)
            {
                _logger.LogInformation(await response.Content.ReadAsStringAsync());
            }
            return response;
        }
    }
}
