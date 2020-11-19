using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace WatchVideoTest
{
    public class GetUps
    {
        [Fact]
        public void GetFollowings()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var cookie = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BiliBiliCookieOptions>>();
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();

                var re = api.GetFollowings(cookie.CurrentValue.UserId, 1, 100).Result;
                var re2 = api.GetFollowings(cookie.CurrentValue.UserId, 1, 200).Result;
                var re3 = api.GetFollowings(cookie.CurrentValue.UserId, 1, int.MaxValue).Result;

                Assert.True(re.Code == 0);
            }
        }

        [Fact]
        public void GetSpecialFollowings()
        {
            Program.PreWorks(new string[] { });

            using (var scope = RayContainer.Root.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();

                var re = api.GetSpecialFollowings(1, int.MaxValue).Result;

                Assert.True(re.Code == 0);
            }
        }
    }
}
