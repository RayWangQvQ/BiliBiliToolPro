using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.CoolPushBatched
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
                content = message,
                topic = _topic,
                template= "markdown"
            }.ToJson();

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
