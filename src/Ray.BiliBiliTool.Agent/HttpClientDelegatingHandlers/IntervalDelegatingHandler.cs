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

        public IntervalDelegatingHandler(IOptionsMonitor<SecurityOptions> securityOptions)
        {
            _securityOptions = securityOptions.CurrentValue;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IntervalForSecurity(request.Method);
            return await base.SendAsync(request, cancellationToken);
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
