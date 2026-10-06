using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Comps;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class RestoreConfigButtonTests : TestContext
{
    private int _restores;

    public RestoreConfigButtonTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<RestoreConfigButton> Button(bool dirty = true, bool busy = false) =>
        RenderComponent<RestoreConfigButton>(parameters =>
            parameters
                .Add(component => component.HasChanges, dirty)
                .Add(component => component.Busy, busy)
                .Add(component => component.OnRestore, () => _restores++)
        );

    [Fact]
    public async Task UnchangedFormRestoresWithoutPrompt()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var button = Button(false);
        await button.Find("button").ClickAsync(new());
        Assert.Equal(1, _restores);
        Assert.Empty(dialogs.FindAll(".mud-dialog"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedFormWaitsForDecisionAndOnlyConfirmationRestores(bool confirm)
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var button = Button();
        var pending = button.Find("button").ClickAsync(new());
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".restore-config-confirm")));
        Assert.Contains("当前页面有未保存的修改", dialogs.Markup);
        Assert.Equal(0, _restores);
        Assert.False(pending.IsCompleted);
        Assert.True(button.Find("button").HasAttribute("disabled"));
        await dialogs
            .Find(confirm ? ".restore-config-confirm" : ".restore-config-cancel")
            .ClickAsync(new());
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(confirm ? 1 : 0, _restores);
        button.WaitForAssertion(() => Assert.False(button.Find("button").HasAttribute("disabled")));
    }

    [Fact]
    public async Task DuplicateClickSharesOneConfirmation()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var button = Button();
        var pending = button.Find("button").ClickAsync(new());
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".restore-config-confirm")));
        await button.Find("button").ClickAsync(new());
        Assert.Single(dialogs.FindAll(".restore-config-confirm"));
        await dialogs.Find(".restore-config-confirm").ClickAsync(new());
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, _restores);
    }

    [Fact]
    public async Task SavingBlocksRestoreEvenWithoutChanges()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var button = Button(false, true);
        Assert.True(button.Find("button").HasAttribute("disabled"));
        await button.Find("button").ClickAsync(new());
        Assert.Equal(0, _restores);
        Assert.Empty(dialogs.FindAll(".mud-dialog"));
    }

    [Fact]
    public async Task DisposalCancelsPromptWithoutRestoring()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var button = Button();
        var pending = button.Find("button").ClickAsync(new());
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".restore-config-confirm")));
        await button.InvokeAsync(button.Instance.Dispose);
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, _restores);
        dialogs.WaitForAssertion(() => Assert.Empty(dialogs.FindAll(".restore-config-confirm")));
    }
}
