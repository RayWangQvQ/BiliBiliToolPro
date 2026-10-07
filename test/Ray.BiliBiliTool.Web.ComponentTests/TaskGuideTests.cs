using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Comps;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class TaskGuideTests : TestContext
{
    public TaskGuideTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData("MangaPrivilegeTaskAppService")]
    [InlineData("Silver2CoinTaskAppService")]
    [InlineData("VipPrivilegeTaskAppService")]
    [InlineData("VipBigPointAppService")]
    public void TasksWithoutExtraOptionsDescribeOnlyScheduleAsConfigurable(string key)
    {
        var cut = RenderComponent<TaskGuide>(p => p.Add(x => x.TaskKey, key));
        var sections = cut.FindAll("dl");
        Assert.Single(sections[0].QuerySelectorAll("dt"));
        Assert.Equal("启用任务与执行时间", sections[0].QuerySelector("dt")!.TextContent);
        Assert.NotEmpty(sections[1].QuerySelectorAll("dt"));
        Assert.Contains("任务步骤自动执行", cut.Find(".task-guide-location").TextContent);
    }

    [Fact]
    public void GuideExplainsHiddenControlsAndUpdatesWhenEnabled()
    {
        var cut = RenderComponent<TaskGuide>(p =>
            p.Add(x => x.TaskKey, "DailyTaskAppService").Add(x => x.Enabled, false)
        );
        Assert.Contains("开启「启用任务」", cut.Find(".task-guide-location").TextContent);
        Assert.DoesNotContain("登录", cut.FindAll("dl")[0].TextContent);
        Assert.Contains("登录", cut.FindAll("dl")[1].TextContent);
        cut.SetParametersAndRender(p => p.Add(x => x.Enabled, true));
        Assert.Contains("下方表单", cut.Find(".task-guide-location").TextContent);
    }
}
