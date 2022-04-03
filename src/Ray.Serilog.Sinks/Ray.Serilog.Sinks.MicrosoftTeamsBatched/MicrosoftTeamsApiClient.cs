using System.Text;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.MicrosoftTeamsBatched
{
    public class MicrosoftTeamsApiClient : PushService
    {
        //https://docs.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-incoming-webhook

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public MicrosoftTeamsApiClient(
            string webhook
            )
        {
            _apiUrl = new Uri(webhook);
        }

        public override string ClientName => "MicrosoftTeams";

        protected override string NewLineStr => "<br/>";

        public override HttpResponseMessage DoSend()
        {
            var json = new
            {
                text=Msg
            }.ToJson();

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
