using System;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.MicrosoftTeamsBatched
{
    public class GotifyApiClient : PushService
    {
        //https://gotify.net/docs

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _token;

        public GotifyApiClient(
            string host,
            string token
            )
        {
            _token = token;
            _apiUrl = new Uri($"{host}/message");
        }

        public override string ClientName => "Gotify";

        protected override string NewLineStr => "\n";

        public override HttpResponseMessage DoSend()
        {
            var json = new
            {
                title = Title,
                message = Msg,
                extras = "{\"extras\":{\"client::display\":{\"contentType\":\"text/markdown\"}}}".ToObject<JObject>()
            }.ToJson();

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Gotify-Key", _token);
            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            response.Content = new StringContent("");
            return response;
        }
    }
}
