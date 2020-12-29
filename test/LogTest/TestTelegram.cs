using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.TelegramBatched;
//using Serilog;
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
            Program.PreWorks(new string[] { });

            _botToken = Global.ConfigurationRoot["Serilog:WriteTo:3:Args:botToken"];
            _chatId = Global.ConfigurationRoot["Serilog:WriteTo:3:Args:chatId"];
        }

        [Fact]
        public void Test2()
        {
            TelegramApiClient client = new TelegramApiClient(_botToken, _chatId);
            var result = client.PushMessageAsync(LogConstants.Msg).Result;
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();

            /*
             * 如果指定markdown，星号会导致推送失败
             */
        }
    }
}
