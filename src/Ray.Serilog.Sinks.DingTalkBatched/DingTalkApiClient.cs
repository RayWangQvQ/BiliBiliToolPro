using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.DingTalkBatched
{
    public class DingTalkApiClient : IPushService
    {
        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public DingTalkApiClient(string webHookUrl)
        {
            _apiUrl = new Uri(webHookUrl);
        }

        public async Task<HttpResponseMessage> PushMessageAsync(string message)
        {
            var json = new { msgtype = "markdown", markdown = new { title = "Ray.BiliBiliTool任务日报", text = message } }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);
            return response;
        }
    }
}
