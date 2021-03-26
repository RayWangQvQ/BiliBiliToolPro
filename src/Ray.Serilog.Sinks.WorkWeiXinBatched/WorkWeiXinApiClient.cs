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
    public class WorkWeiXinApiClient : IPushService
    {
        //https://work.weixin.qq.com/api/doc/90000/90136/91770

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public WorkWeiXinApiClient(string webHookUrl)
        {
            _apiUrl = new Uri(webHookUrl);
        }

        public override string Name => "企业微信";

        public override HttpResponseMessage PushMessage(string message)
        {
            base.PushMessage(message);

            var json = new
            {
                msgtype = WorkWeiXinMsgType.Markdown.ToString().ToLower(),
                markdown = new
                {
                    content = BuildMsg(message)
                }
            }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;

        }

        /*
         * 不能用<br/>换行
         */
    }


    public enum WorkWeiXinMsgType
    {
        Text,
        Markdown,
        Image,
        News,
        File
    }
}
