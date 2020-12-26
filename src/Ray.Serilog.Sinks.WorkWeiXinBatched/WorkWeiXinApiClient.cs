using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.WorkWeiXinBatched
{
    public class WorkWeiXinApiClient : IPushService
    {
        private readonly Uri apiUrl;
        private readonly HttpClient httpClient = new HttpClient();

        public WorkWeiXinApiClient(string webHookUrl)
        {
            apiUrl = new Uri(webHookUrl);
        }

        public async Task<HttpResponseMessage> PushMessageAsync(string message)
        {
            var json = new { msgtype = "markdown", markdown = new { content = message } }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
            return response;
        }
    }
}
