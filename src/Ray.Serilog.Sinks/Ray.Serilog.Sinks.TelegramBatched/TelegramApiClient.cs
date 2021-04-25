using System;
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
        /// Initializes a new instance of the <see cref="TelegramClient"/> class.
        /// </summary>
        /// <param name="botToken">The Telegram bot token.</param>
        /// <param name="timeoutSeconds">The timeout seconds.</param>
        /// <exception cref="ArgumentException">Thrown if the bot token is null or empty.</exception>
        public TelegramApiClient(string botToken, string chatId, int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                SelfLog.WriteLine("The bot token mustn't be empty.");
                throw new ArgumentException("The bot token mustn't be empty.", nameof(botToken));
            }

            _chatId = chatId;

            this._apiUrl = new Uri($"{TelegramBotApiUrl}{botToken}/sendMessage");
            this._httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public override string ClientName => "Telegram机器人";

        public override HttpResponseMessage DoSend()
        {
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
    }

    public enum TeleMsgType
    {
        MarkdownV2,
        HTML,
        Markdown,
    }
}
