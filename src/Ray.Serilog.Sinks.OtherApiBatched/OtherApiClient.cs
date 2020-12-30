using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.OtherApiBatched
{
    public class OtherApiClient : IPushService
    {
        private readonly Uri _apiUri;
        private readonly string _json;
        private readonly string _placeholder;

        private readonly HttpClient _httpClient = new HttpClient();


        public OtherApiClient(string apiUrl, string json, string placeholder)
        {
            _json = json;
            _placeholder = placeholder;
            _apiUri = new Uri(apiUrl);
        }

        public async Task<HttpResponseMessage> PushMessageAsync(string message)
        {
            message = message.ToJson();
            var json = _json.Replace(_placeholder, message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await this._httpClient.PostAsync(_apiUri, content);
            return response;
        }
    }
}
