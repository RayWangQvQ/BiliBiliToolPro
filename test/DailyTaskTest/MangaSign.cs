using DailyTaskTest.Share;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace MangaSignTest
{
    public class MangaSign
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var dailyTask = DailyTaskBuilder.Build();

            dailyTask.MangaSign();

            Assert.True(true);
        }
    }
}
