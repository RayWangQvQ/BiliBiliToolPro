using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Xunit;

namespace DailyTaskTest
{
    public class RelationTags
    {
        public RelationTags()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });
        }

        [Fact]
        public void GetTags()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();
                var cookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

                string referer = string.Format(RelationApiConstant.GetTagsReferer, cookie.UserId);
                var tags = api.GetTags(referer)
                    .GetAwaiter().GetResult();

                Debug.WriteLine(JsonSerializer.Serialize(tags));
            }
        }


        [Fact]
        public void CreateTag()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();
                var cookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

                string referer = string.Format(RelationApiConstant.GetTagsReferer, cookie.UserId);

                var request = new CreateTagRequest
                {
                    Tag = "测试",
                    Csrf = cookie.BiliJct
                };

                var re = api.CreateTag(request, referer)
                    .GetAwaiter().GetResult();

                Debug.WriteLine(JsonSerializer.Serialize(re));
            }
        }

        [Fact]
        public void CopyUpsToGroup()
        {
            using (var scope = Global.ServiceProviderRoot.CreateScope())
            {
                var api = scope.ServiceProvider.GetRequiredService<IRelationApi>();
                var cookie = scope.ServiceProvider.GetRequiredService<BiliCookie>();

                //获取最近关注的前2个up
                var ups = api.GetFollowings(new GetFollowingsRequest(long.Parse(cookie.UserId), FollowingsOrderType.TimeDesc))
                    .GetAwaiter().GetResult();
                var followingIds = ups.Data.List.Take(2).Select(x => x.Mid);

                string referer = string.Format(RelationApiConstant.GetTagsReferer, cookie.UserId);

                //获取天选时刻分组
                var groups = api.GetTags(referer).GetAwaiter().GetResult();
                int tagId = groups.Data.Find(x => x.Name == "天选时刻")?.Tagid ?? 0;

                var re = api.CopyUpsToGroup(new CopyUserToGroupRequest(followingIds.ToList(), tagId.ToString(), cookie.BiliJct), referer)
                    .GetAwaiter().GetResult();

                Debug.WriteLine(JsonSerializer.Serialize(re));
            }
        }
    }
}
