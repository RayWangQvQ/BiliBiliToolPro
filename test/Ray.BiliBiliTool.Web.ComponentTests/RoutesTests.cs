using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class RoutesTests : TestContext
{
    [Fact]
    public void UnauthorizedAdmin_LoginButtonNavigatesToLogin()
    {
        Services.AddMudServices();
        Services.AddSingleton<IAppInfoProvider>(new FakeAppInfoProvider("test"));
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTestAuthorization().SetNotAuthorized();
        ((IServiceProvider)Services).GetRequiredService<NavigationManager>().NavigateTo("/Admin");

        var cut = RenderComponent<Routes>();

        var loginButton = cut.FindAll("button, a")
            .Single(element => element.TextContent.Trim() == "前往登录");
        loginButton.TagName.Should().Be("A");
        loginButton.GetAttribute("href").Should().Be("/login");
    }
}
