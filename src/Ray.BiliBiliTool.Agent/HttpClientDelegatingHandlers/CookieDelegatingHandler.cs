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
        private readonly BiliCookie _biliCookie;

        public CookieDelegatingHandler(ILogger<CookieDelegatingHandler> logger, BiliCookie biliCookie)
        {
            _logger = logger;
            _biliCookie = biliCookie;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ReplaceCookieContainerWithCurrentAccount(this.CookieContainer);

            HttpResponseMessage re = await base.SendAsync(request, cancellationToken);

            UpdateCurrentCookieContainer(this.CookieContainer);

            return re;
        }

        public void ReplaceCookieContainerWithCurrentAccount(CookieContainer source)
        {
            //clear existed by Expiring it
            source.GetAllCookies().ToList().ForEach(x => x.Expired = true);
            if (source.Count > 0)
            {
                var m = source.GetType().GetMethod("AgeCookies", BindingFlags.NonPublic | BindingFlags.Instance);
                m.Invoke(source, new object[] { null });
            }

            //add new
            //source.Add(this.CurrentTargetAccount.MyCookieContainer.GetAllCookies());
            //todo
        }

        public void UpdateCurrentCookieContainer(CookieContainer update)
        {
            var newCookieContainer = new CookieContainer();
            newCookieContainer.Add(update.GetAllCookies());
            //this.CurrentTargetAccount.MyCookieContainer = newCookieContainer;
            //todo
        }
    }
}
