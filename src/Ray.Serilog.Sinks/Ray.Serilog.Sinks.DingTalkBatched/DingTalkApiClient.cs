using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.DingTalkBatched
{
    public class DingTalkApiClient : PushService
    {
        //https://developers.dingtalk.com/document/app/overview-of-group-robots

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public DingTalkApiClient(string webHookUrl)
        {
            _apiUrl = new Uri(webHookUrl);
        }

        public override string ClientName => "钉钉机器人";

        /// <summary>
        /// <br/>换行无效
        /// 文档里是\n换行
        /// 只能换1行
        /// </summary>
        protected override string NewLineStr => Environment.NewLine + Environment.NewLine;

        public override void BuildMsg()
        {
            //附加标题
            Msg = $"## {Title} {Environment.NewLine}{Msg}";
            base.BuildMsg();
        }

        public override HttpResponseMessage DoSend()
        {
            var json = new
            {
                msgtype = DingMsgType.markdown.ToString(),
                markdown = new
                {
                    title = Title,
                    text = Msg
                }
            }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }

    public enum DingMsgType
    {
        text,
        markdown,
        actionCard,
        feedCard,
        empty
    }
}
