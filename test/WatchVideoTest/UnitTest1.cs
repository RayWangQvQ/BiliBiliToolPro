using System;
using BiliBiliTool;
using BiliBiliTool.Task;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WatchVideoTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            DailyTask dailyTask = Program.ServiceProviderRoot.GetRequiredService<DailyTask>();

            string aid = dailyTask.GetRandomVideo();
            dailyTask.WatchVideo(aid);

            Assert.True(true);
        }
    }
}
