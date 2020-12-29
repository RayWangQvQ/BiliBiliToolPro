using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public class ServerChanApiClient : IPushService
    {
        private const string Host = "http://sc.ftqq.com";

        private readonly Uri _apiUrl;
        private readonly string _title;
        private readonly HttpClient _httpClient = new HttpClient();

        public ServerChanApiClient(string scKey, string title = "Ray.BiliBiliTool任务日报")
        {
            _title = title;
            _apiUrl = new Uri($"{Host}/{scKey}.send");
        }

        public async Task<HttpResponseMessage> PushMessageAsync(string message)
        {
            var dic = new Dictionary<string, string>
            {
                {"text", _title},
                {"desp", message}
            };
            var content = new FormUrlEncodedContent(dic);
            var response = await _httpClient.PostAsync(_apiUrl, content);
            return response;
        }
    }
}
