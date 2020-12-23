using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Serilog.Sinks.Telegram;
using Serilog;
using Xunit;

namespace LogTest
{
    public class TestTelegram
    {
        private static string _botToken = "";
        private static string _chatId = "";

        [Fact]
        public void Test1()
        {
            ILogger logger = CreateLogger();

            int count = 10;
            for (int i = 0; i < count; i++)
            {
                if (i == 5)
                    logger.Information("---{num}---", i.ToString());
                else
                    logger.Information(i.ToString());
                Thread.Sleep(1000);
            }
            logger.Information("---");
            System.Console.ReadLine();
        }

        private ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.TelegramBatched(
                _botToken,
                _chatId,
                x => x.MessageTemplate.Text.StartsWith("---"),
                false)
                .CreateLogger();
        }
    }
}
