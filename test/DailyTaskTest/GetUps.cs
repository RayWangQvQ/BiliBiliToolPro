using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class GetUps
    {
        public GetUps()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void GetFollowings()
        {
            Program.CreateHost(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var cookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();

                var request = new GetFollowingsRequest(long.Parse(cookie.UserId));

                request.Ps = 100;
                var re = api.GetFollowings(request).Result;

                request.Ps = 200;
                var re2 = api.GetFollowings(request).Result;

                request.Ps = int.MaxValue;
                var re3 = api.GetFollowings(request).Result;

                Assert.True(re.Code == 0);
            }
        }

        [Fact]
        public void GetSpecialFollowings()
        {
            Program.CreateHost(new string[] { });

            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();
                var cookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

                var request = new GetSpecialFollowingsRequest(long.Parse(cookie.UserId));
                var re = api.GetFollowingsByTag(request).Result;

                Assert.True(re.Code == 0);
            }
        }
    }
}
