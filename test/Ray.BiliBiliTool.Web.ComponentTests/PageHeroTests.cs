using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Comps;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class PageHeroTests : TestContext
{
    public PageHeroTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(Size.Large, "app-hero-large")]
    [InlineData(Size.Medium, "app-hero-medium")]
    [InlineData(Size.Small, "app-hero-small")]
    public void PageHero_RendersAccessibleHeadingAndTier(Size size, string cssClass)
    {
        var cut = RenderComponent<PageHero>(parameters =>
            parameters
                .Add(p => p.Size, size)
                .Add(p => p.Title, "页面标题")
                .Add(p => p.Description, "页面说明")
        );

        cut.Find("h1").TextContent.Should().Be("页面标题");
        cut.Find(".app-hero-description").TextContent.Should().Be("页面说明");
        cut.Find(".app-hero").ClassList.Should().Contain(cssClass);
    }

    [Fact]
    public void PageHero_RendersExistingActionsWhenProvided()
    {
        var cut = RenderComponent<PageHero>(parameters =>
            parameters
                .Add(p => p.Title, "账号管理")
                .Add(p => p.Description, "管理账号")
                .Add(
                    p => p.Actions,
                    builder => builder.AddMarkupContent(0, "<button>添加账号</button>")
                )
        );

        cut.Find(".app-hero-actions button").TextContent.Should().Be("添加账号");
    }
}
