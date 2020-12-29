using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.CoolPushBatched
{
    public class CoolPushApiClient : IPushService
    {
        private const string Host = "https://push.xuthus.cc/send";

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public CoolPushApiClient(string sKey)
        {
            _apiUrl = new Uri($"{Host}/{sKey}");
        }

        public async Task<HttpResponseMessage> PushMessageAsync(string message)
        {
            /*
            var dic = new Dictionary<string, string>
            {
                {"c", message}
            };
            var content = new FormUrlEncodedContent(dic);
            */

            var content = new StringContent(message, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            return response;
        }
    }
}
