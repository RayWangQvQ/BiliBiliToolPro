using DailyTaskTest.Share;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Xunit;

namespace MangaSignTest
{
    public class MangaSign
    {
        [Fact]
        public void Test1()
        {
            Program.PreWorks(new string[] { });

            using (var scope = Program.ServiceProviderRoot.CreateScope())
            {
                var dailyTask = scope.ServiceProvider.GetRequiredService<IMangaDomainService>();

                dailyTask.MangaSign();

                Assert.True(true);
            }
        }
    }
}
