using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Pages.BiliAccount;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public sealed class EditAccountSaveTests : TestContext
{
    public EditAccountSaveTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task CookieEditDisablesUnchangedAndRetainsFailedDraftUntilSaveSucceeds()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var parameters = new DialogParameters<EditAccountDialog>
        {
            { x => x.CookieStr, "DedeUserID=1001; SESSDATA=synthetic-original" },
            {
                x => x.OnSave,
                (Func<string, Task>)(
                    _ =>
                    {
                        calls++;
                        return pending.Task;
                    }
                )
            },
        };
        var service = ((IServiceProvider)Services).GetRequiredService<IDialogService>();
        var dialog = await provider.InvokeAsync(() =>
            service.ShowAsync<EditAccountDialog>("编辑账号", parameters)
        );
        provider.WaitForAssertion(() => Assert.Single(provider.FindAll(".save-changes-button")));
        Assert.True(provider.Find(".save-changes-button").HasAttribute("disabled"));
        var input = provider.Find("textarea");
        input.Input("DedeUserID=1001; SESSDATA=synthetic-replacement");
        Assert.False(provider.Find(".save-changes-button").HasAttribute("disabled"));
        input.Input("DedeUserID=1001; SESSDATA=synthetic-original");
        Assert.True(provider.Find(".save-changes-button").HasAttribute("disabled"));
        input.Input("DedeUserID=1001; SESSDATA=synthetic-replacement");
        var save = provider.Find(".save-changes-button").ClickAsync(new());
        provider.WaitForAssertion(() => Assert.Contains("正在保存", provider.Markup));
        Assert.Equal(1, calls);
        Assert.True(provider.Find(".save-changes-button").HasAttribute("disabled"));
        await provider.InvokeAsync(() =>
            pending.SetException(new IOException("synthetic internal details"))
        );
        await save;
        Assert.Contains("保存失败，修改已保留，请重试", provider.Markup);
        Assert.DoesNotContain("synthetic internal details", provider.Markup);
        Assert.False(provider.Find(".save-changes-button").HasAttribute("disabled"));
        pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        save = provider.Find(".save-changes-button").ClickAsync(new());
        await provider.InvokeAsync(() => pending.SetResult());
        await save;
        var result = await dialog.Result;
        Assert.False(result!.Canceled);
        Assert.Equal("DedeUserID=1001; SESSDATA=synthetic-replacement", result.Data);
    }
}
