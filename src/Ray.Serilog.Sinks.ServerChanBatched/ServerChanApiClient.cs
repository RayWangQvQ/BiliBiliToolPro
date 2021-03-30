using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public class ServerChanApiClient : PushService
    {
        //http://sc.ftqq.com/9.version

        private const string Host = "http://sc.ftqq.com";

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public ServerChanApiClient(string scKey)
        {
            _apiUrl = new Uri($"{Host}/{scKey}.send");
        }

        public override string ClientName => "Server酱";
        public override string BuildMsg()
        {
            //return msg.Replace(Environment.NewLine, "<br/>");//无效
            Msg = Msg.Replace(Environment.NewLine, Environment.NewLine + Environment.NewLine);


            Msg += $"{Environment.NewLine}{Environment.NewLine}### 检测到当前为老版Server酱,即将失效,建议更换其他推送方式或更新至Server酱Turbo版";

            return Msg;
        }

        public override HttpResponseMessage DoSend()
        {
            var dic = new Dictionary<string, string>
            {
                {"text", Title},
                {"desp", Msg}
            };
            var content = new FormUrlEncodedContent(dic);
            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
