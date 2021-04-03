using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.ServerChanBatched
{
    public class ServerChanTurboApiClient : PushService
    {
        //http://sc.ftqq.com/9.version

        private const string Host = "https://sctapi.ftqq.com";

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public ServerChanTurboApiClient(string scKey)
        {
            _apiUrl = new Uri($"{Host}/{scKey}.send");
        }

        public override string ClientName => "Server酱Turbo版";

        /// <summary>
        /// 需要两个才可以换行
        /// 只能换单行
        /// <br/>无效
        /// </summary>
        protected override string NewLineStr => Environment.NewLine + Environment.NewLine;

        public override HttpResponseMessage DoSend()
        {
            var dic = new Dictionary<string, string>
            {
                {"title", Title},//标题必填
                {"desp", Msg}
            };
            var content = new FormUrlEncodedContent(dic);
            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }
}
