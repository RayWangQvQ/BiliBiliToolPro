using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.WorkWeiXinBatched
{
    public class WorkWeiXinApiClient : PushService
    {
        //https://work.weixin.qq.com/api/doc/90000/90136/91770

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public WorkWeiXinApiClient(string webHookUrl)
        {
            _apiUrl = new Uri(webHookUrl);
        }

        public override string ClientName => "企业微信机器人";

        public override string BuildMsg()
        {
            //附加标题
            Msg = $"## {Title} {Environment.NewLine}{Msg}";

            return base.BuildMsg();

            /*
             * 不能用<br/>换行
             * 可以多行换行
             */
        }

        public override HttpResponseMessage DoSend()
        {
            var json = new
            {
                msgtype = WorkWeiXinMsgType.markdown.ToString(),
                markdown = new
                {
                    content = Msg
                }
            }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }


    public enum WorkWeiXinMsgType
    {
        text,
        markdown,
        image,
        news,
        file
    }
}
