using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Components.Layout;
using Ray.BiliBiliTool.Web.Components.Pages.Today;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class TaskCatalogNavigationTests : TestContext
{
    [Fact]
    public void NavigationAndTodayShowEveryConfiguredTaskInTheSameOrder()
    {
        Services.AddMudServices();
        Services.Configure<AutoRecoverOptions>(_ => { });
        JSInterop.Mode = JSRuntimeMode.Loose;
        var fake = DispatchProxy.Create<ITodayTaskService, TodayMedalDisplayTests.TodayProxy>();
        ((TodayMedalDisplayTests.TodayProxy)fake).Accounts =
        [
            new()
            {
                UserId = 1001,
                UserName = "示例账号",
                Index = 0,
                IsCookieValid = true,
                Groups = TaskCatalog
                    .All.Select(task => new TodayTaskGroupDto
                    {
                        TaskKey = task.TaskKey,
                        DisplayName = task.DisplayName,
                        Items = task
                            .Items.Select(item => new TodayTaskItemDto
                            {
                                ItemKey = item.ItemKey,
                                DisplayName = item.DisplayName,
                                State = TodayTaskItemState.NotDone,
                                StateText = "未完成",
                            })
                            .ToList(),
                    })
                    .ToList(),
            },
        ];
        Services.AddSingleton(fake);
        var nav = RenderComponent<NavMenu>();
        var links = nav.FindAll("a[href^='/Configurations/']")
            .Where(link => !link.GetAttribute("href")!.EndsWith("NotificationConfig"))
            .ToList();
        Assert.Equal(
            new[]
            {
                "每日任务",
                "漫画任务",
                "漫画特权",
                "银瓜子换硬币",
                "充电任务",
                "大会员福利",
                "大会员积分",
                "直播抽奖",
                "粉丝勋章",
                "批量取关",
            },
            links.Select(link => link.TextContent.Trim())
        );
        Assert.Equal(
            TaskCatalog.All.Select(task => TaskHelpCatalog.ConfigurationUrl(task.TaskKey)),
            links.Select(link => link.GetAttribute("href"))
        );
        var page = RenderComponent<Today>();
        Assert.Equal(10, page.FindAll(".task-group").Count);
        Assert.Equal(
            1,
            page.FindAll(".today-task-heading > p")
                .Count(heading => heading.TextContent == "大会员福利")
        );
        Assert.DoesNotContain("大会员福利", page.FindAll(".task-group")[0].TextContent);
        Assert.Contains(
            "/Configurations/VipPrivilegeConfig",
            page.FindAll(".task-group")[5].InnerHtml
        );
        Assert.Equal(13, page.FindAll(".today-task-row").Count);
    }
}
