using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
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
                    Tag = "≤‚ ‘",
                    Csrf = cookie.BiliJct
                };

                var re = api.CreateTag(request, referer)
                    .GetAwaiter().GetResult();

                Debug.WriteLine(JsonSerializer.Serialize(re));
            }
        }
    }
}
