using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ray.Serilog.Sinks.Batched;
using Serilog.Debugging;

namespace Ray.Serilog.Sinks.TelegramBatched
{
    public class TelegramApiClient : PushService
    {
        //https://core.telegram.org/bots/api#available-methods

        private readonly string _chatId;
        private readonly string _proxy;
        private const string TelegramBotApiUrl = "https://api.telegram.org/bot";

        /// <summary>
        /// The API URL.
        /// </summary>
        private readonly Uri _apiUrl;

        /// <summary>
        /// The HTTP client.
        /// </summary>
        private readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="TelegramApiClient"/> class.
        /// </summary>
        /// <param name="botToken">The Telegram bot token.</param>
        /// <param name="timeoutSeconds">The timeout seconds.</param>
        /// <exception cref="ArgumentException">Thrown if the bot token is null or empty.</exception>
        public TelegramApiClient(string botToken, string chatId, string proxy="", int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                SelfLog.WriteLine("The bot token mustn't be empty.");
                throw new ArgumentException("The bot token mustn't be empty.", nameof(botToken));
            }

            _chatId = chatId;
            _proxy = proxy;

            this._apiUrl = new Uri($"{TelegramBotApiUrl}{botToken}/sendMessage");

            if (proxy.IsNotNullOrEmpty())
            {
                var webProxy = GetWebProxy(proxy);
                var proxyHttpClientHandler = new HttpClientHandler
                {
                    Proxy = webProxy,
                    UseProxy = true,
                };
                _httpClient = new HttpClient(proxyHttpClientHandler);
            }

            this._httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public override string ClientName => "Telegram机器人";

        public override HttpResponseMessage DoSend()
        {
            SelfLog.WriteLine($"使用代理：{_proxy.IsNotNullOrEmpty()}");

            var json = new
            {
                chat_id = _chatId,
                text = Msg,
                parse_mode = TeleMsgType.HTML.ToString(),
                disable_web_page_preview = true
            }.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = this._httpClient.PostAsync(this._apiUrl, content).GetAwaiter().GetResult();
            return response;
        }

        public override void BuildMsg()
        {
            //附加标题
            Msg = $"<b>{Title}</b>{Environment.NewLine}{Environment.NewLine}{Msg}";

            base.BuildMsg();
        }

        private WebProxy GetWebProxy(string proxyAddress)
        {
            //todo:抽象到公共方法库
            WebProxy webProxy;

            //user:password@host:port http proxy only .Tested with tinyproxy-1.11.0-rc1
            if (proxyAddress.Contains("@"))
            {
                string userPass = proxyAddress.Split("@")[0];
                string address = proxyAddress.Split("@")[1];

                string proxyUser = userPass.Split(":")[0];
                string proxyPass = userPass.Split(":")[1];

                var credentials = new NetworkCredential(proxyUser, proxyPass);

                webProxy = new WebProxy(address, true,null, credentials);
            }
            else
            {
                webProxy = new WebProxy(proxyAddress, true);
            }

            return webProxy;
        }
    }

    public enum TeleMsgType
    {
        MarkdownV2,
        HTML,
        Markdown,
    }
}
