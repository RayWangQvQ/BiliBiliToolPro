using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.CoolPushBatched
{
    public class CoolPushApiClient : PushService
    {
        private const string Host = "https://push.xuthus.cc/send";

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public CoolPushApiClient(string sKey)
        {
            _apiUrl = new Uri($"{Host}/{sKey}");
        }

        public override string ClientName => "酷推";

        protected override string NewLineStr => Environment.NewLine + Environment.NewLine;

        public override void BuildMsg()
        {
            //附加标题
            Msg = Title + Environment.NewLine + Msg;

            base.BuildMsg();
        }

        public override HttpResponseMessage DoSend()
        {
            var content = new StringContent(Msg, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
