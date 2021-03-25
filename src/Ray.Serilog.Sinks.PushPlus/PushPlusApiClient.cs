using System;
using System.Net.Http;
using System.Text;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.PushPlus
{
    public class PushPlusApiClient : IPushService
    {
        private const string Host = "http://www.pushplus.plus/send";

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _token;
        private readonly string _topic;

        public PushPlusApiClient(string token, string topic = null)
        {
            _apiUrl = new Uri(Host);
            _token = token;
            _topic = topic;
        }

        public override string Name => "PushPlus";

        public override HttpResponseMessage PushMessage(string message)
        {
            base.PushMessage(message);

            var json = new
            {
                token = _token,
                title = "Ray.BiliBiliTool任务日报",
                content = message.Replace("\r\n", "<br/>"),//换行有问题，这里使用<br/>替换\r\n
                //content = message,
                topic = _topic,
                template = "html"
            }.ToJson();

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
