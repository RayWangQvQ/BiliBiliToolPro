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

        /// <summary>
        /// 需要两个才可以换行
        /// 只能换单行
        /// <br/>无效
        /// </summary>
        protected override string NewLineStr => Environment.NewLine + Environment.NewLine;

        public override void BuildMsg()
        {
            Msg += $"{Environment.NewLine}### 检测到当前为老版Server酱,即将失效,建议更换其他推送方式或更新至Server酱Turbo版";

            base.BuildMsg();
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
