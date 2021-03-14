using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers
{
    /// <summary>
    /// HttpClient切面
    /// </summary>
    public class IntervalDelegatingHandler : DelegatingHandler
    {
        private readonly SecurityOptions _securityOptions;

        private readonly Dictionary<string, int> _special = new Dictionary<string, int>() {
            {"/xlive/lottery-interface/v1/Anchor/Join",3 }//天选抽奖，有时效，不能间隔过久，使用默认3秒
        };

        public IntervalDelegatingHandler(IOptionsMonitor<SecurityOptions> securityOptions)
        {
            _securityOptions = securityOptions.CurrentValue;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IntervalForSecurity(request);
            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// 安全间隔
        /// </summary>
        /// <param name="method"></param>
        private void IntervalForSecurity(HttpRequestMessage request)
        {
            if (_securityOptions.IntervalSecondsBetweenRequestApi <= 0) return;
            if (!_securityOptions.GetIntervalMethods().Contains(request.Method)) return;

            int seconds = 0;
            //需要特殊处理的接口
            if (_special.TryGetValue(request.RequestUri.AbsolutePath, out int s))
            {
                seconds = s;
            }
            else
            {
                int maxSeconds = _securityOptions.IntervalSecondsBetweenRequestApi;
                seconds = new Random().Next(maxSeconds / 2, maxSeconds + 1);
            }

            Task.Delay(seconds * 1000).Wait();
        }
    }
}
