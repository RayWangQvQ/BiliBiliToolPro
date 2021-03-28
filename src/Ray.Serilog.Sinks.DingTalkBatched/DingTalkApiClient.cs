using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.DingTalkBatched
{
    public class DingTalkApiClient : IPushService
    {
        //https://developers.dingtalk.com/document/app/overview-of-group-robots

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public DingTalkApiClient(string webHookUrl)
        {
            _apiUrl = new Uri(webHookUrl);
        }

        public override string Name => "钉钉";

        public override HttpResponseMessage PushMessage(string message)
        {
            base.PushMessage(message);

            var json = new
            {
                msgtype = DingMsgType.markdown.ToString(),
                markdown = new
                {
                    title = "Ray.BiliBiliTool任务日报",
                    text = BuildMsg(message)
                }
            }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }

        public override string BuildMsg(string msg)
        {
            //return msg.Replace(Environment.NewLine, "<br/>");//无效
            return msg.Replace(Environment.NewLine, Environment.NewLine + Environment.NewLine);
            //return msg.Replace(Environment.NewLine, "\n\n");//文档里是\n

            /*
             * 只能换1行
             */
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
