using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public class ServerChanTurboApiClient : IPushService
    {
        //http://sc.ftqq.com/9.version

        private const string Host = "https://sctapi.ftqq.com";

        private readonly Uri _apiUrl;
        private readonly string _title;
        private readonly HttpClient _httpClient = new HttpClient();

        public ServerChanTurboApiClient(string scKey, string title = "Ray.BiliBiliTool任务日报")
        {
            _title = title;
            _apiUrl = new Uri($"{Host}/{scKey}.send");
        }

        public override string Name => "Server酱Turbo版";

        public override HttpResponseMessage PushMessage(string message)
        {
            base.PushMessage(message);
            var dic = new Dictionary<string, string>
            {
                {"title", _title},
                {"desp", BuildMsg(message)}
            };
            var content = new FormUrlEncodedContent(dic);
            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }

        public override string BuildMsg(string msg)
        {
            //return msg.Replace(Environment.NewLine, "<br/>");//无效
            return msg.Replace(Environment.NewLine, Environment.NewLine + Environment.NewLine);

            /*
             * 只能换1行
             */
        }
    }
}
