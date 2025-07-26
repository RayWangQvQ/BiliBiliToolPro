using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.TelegramBatched;
using Xunit;

namespace LogTest
{
    public class TestTelegram
    {
        private string _botToken;
        private string _chatId;

        public TestTelegram()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(["ENVIRONMENT=Development"]);

            _botToken = Global.ConfigurationRoot["Serilog:WriteTo:3:Args:botToken"];
            _chatId = Global.ConfigurationRoot["Serilog:WriteTo:3:Args:chatId"];
        }

        [Fact]
        public async Task Test2()
        {
            var client = new TelegramApiClient(_botToken, _chatId);

            string msg = LogConstants.Msg2;

            var result = await client.PushMessageAsync(msg, "标题");
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            /*
             * 如果指定markdown，星号会导致推送失败
             */
        }
    }
}
