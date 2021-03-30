using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.OtherApiBatched
{
    public class OtherApiClient : PushService
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

        public override string ClientName => "自定义";
        public override string BuildMsg()
        {
            var json = _json.Replace(_placeholder, Msg.ToJson());
            return json;
        }

        public override HttpResponseMessage DoSend()
        {
            var content = new StringContent(Msg, Encoding.UTF8, "application/json");
            var response = this._httpClient.PostAsync(_apiUri, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
