using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Helpers;
using Xunit;

namespace DailyTaskTest
{
    public class LiveTianXuan
    {
        public LiveTianXuan()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void GetAreaList()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

                var re = api.GetAreaList().Result;

                Assert.True(true);
            }
        }

        [Fact]
        public void GetLiveList()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

                var re = api.GetList(2, 1).Result;

                Assert.True(true);
            }
        }

        [Fact]
        public void Check()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();

                var re = api.CheckTianXuan(22566984).Result;

                Assert.True(true);
            }
        }

        [Fact]
        public void Join()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                int roomId = 22835698;

                var checkApi = scope.ServiceProvider.GetRequiredService<ILiveApi>();
                var check = checkApi.CheckTianXuan(roomId).Result.Data;

                var api = scope.ServiceProvider.GetRequiredService<ILiveApi>();
                var request = new JoinTianXuanRequest
                {
                    Id = check.Id,
                    Gift_id = check.Gift_id,
                    Gift_num = check.Gift_num,
                    Csrf = scope.ServiceProvider.GetRequiredService<BiliCookie>().BiliJct
                };
                var re = api.Join(request).Result;

                Debug.WriteLine(re.ToJson());
                Assert.True(true);
            }
        }

        [Fact]
        public void TianXuan()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<ILiveDomainService>();
                api.TianXuan();
                Assert.True(true);
            }
        }

        [Fact]
        public void GetRandomVisitId()
        {
            var r = new RandomHelper();
            var re = r.GenerateCode(10);
        }

        [Fact]
        public void FilterJoinTianXuan()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<LiveLotteryTaskOptions>>();

                var check = new CheckTianXuanDto
                {
                    Award_name = "ºì°ü"
                };
                var re = check.AwardNameIsSatisfied(options.CurrentValue.IncludeAwardNameList, options.CurrentValue.ExcludeAwardNameList);
                Assert.True(re);
            }
        }
    }
}
