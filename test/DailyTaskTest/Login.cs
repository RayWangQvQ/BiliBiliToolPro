using DailyTaskTest.Share;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace LoginTest
{
    public class Login
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var dailyTask = DailyTaskBuilder.Build();
            dailyTask.Login();

            Assert.NotNull("");
        }
    }
}
