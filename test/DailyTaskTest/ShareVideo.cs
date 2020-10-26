using DailyTaskTest.Share;
using Ray.BiliBiliTool.Console;
using Xunit;

namespace ShareVideoTest
{
    public class ShareVideo
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            var dailyTask = DailyTaskBuilder.Build();

            string aid = dailyTask.GetRandomVideo();
            dailyTask.ShareVideo(aid);

            Assert.True(true);
        }
    }
}
