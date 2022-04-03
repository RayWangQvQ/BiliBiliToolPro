using System.Text;
using Ray.Serilog.Sinks.Batched;

namespace Ray.Serilog.Sinks.WorkWeiXinAppBatched
{
    public class WorkWeiXinAppApiClient : PushService
    {
        // https://developer.work.weixin.qq.com/tutorial/application-message
        // https://developer.work.weixin.qq.com/document/34479
        // https://github.com/JeffreySu/WeiXinMPSDK

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _corpId;
        private readonly string _agentId;
        private readonly string _secret;

        private readonly string _toUser;
        private readonly string _toParty;
        private readonly string _toTag;

        public WorkWeiXinAppApiClient(
            string corpid,
            string agentId,
            string secret,
            string toUser = "",
            string toParty = "",
            string toTag = ""
            )
        {

            _corpId = corpid;
            _agentId = agentId;
            _secret = secret;
            _toUser = toUser;
            _toParty = toParty;
            _toTag = toTag;

            // token
            var token = GetAccessToken(corpid, secret);
            _apiUrl = new Uri($"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}");
        }

        public override string ClientName => "WorkWeiXinApp";

        protected override string NewLineStr => "\n";

        public override HttpResponseMessage DoSend()
        {
            var json = new
            {
                touser = _toUser,
                toparty = _toParty,
                totag = _toTag,
                agentid = _agentId,
                msgtype = "text",
                text = new
                {
                    content = Msg
                }
            }.ToJson();

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }

        private string GetAccessToken(string corpId, string secret)
        {
            var token = "";

            try
            {
                var uri = new Uri($"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={corpId}&corpsecret={secret}");
                var response = _httpClient.GetAsync(uri).GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync()
                            .GetAwaiter().GetResult();

                var re = content.ToObject<WorkWeiXinAppTokenResponse>();

                if (re.errcode == 0) return re.access_token;
            }
            catch (Exception)
            {
                //ignore
            }

            return token;
        }
    }
}
