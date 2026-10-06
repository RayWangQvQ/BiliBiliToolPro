using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Comps;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public sealed class UnsavedChangesNavigationTests : TestContext
{
    private readonly TestNavigationManager _navigation = new();
    private int _saves;

    public UnsavedChangesNavigationTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<NavigationManager>(_navigation);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<UnsavedChangesGuard> Guard(
        bool dirty = true,
        Func<Task<bool>>? save = null
    ) =>
        RenderComponent<UnsavedChangesGuard>(p =>
            p.Add(x => x.HasChanges, dirty)
                .Add(
                    x => x.SaveBeforeLeaving,
                    save
                        ?? (
                            () =>
                            {
                                _saves++;
                                return Task.FromResult(true);
                            }
                        )
                )
        );

    private async Task<Task<bool>> BeginNavigationAsync(
        IRenderedComponent<UnsavedChangesGuard> guard,
        string target
    )
    {
        Task<bool>? navigation = null;
        await guard.InvokeAsync(() =>
        {
            navigation = _navigation.NavigateAsync(target);
        });
        return navigation!.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CleanAndRevertedDraftsNavigateWithoutPromptOrSave()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var guard = Guard(false);
        Assert.True(await await BeginNavigationAsync(guard, "/clean"));
        guard.SetParametersAndRender(p => p.Add(x => x.HasChanges, true));
        Assert.True(guard.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation);
        guard.SetParametersAndRender(p => p.Add(x => x.HasChanges, false));
        Assert.True(await await BeginNavigationAsync(guard, "/reverted"));
        Assert.Empty(dialogs.FindAll(".mud-dialog"));
        Assert.Equal(0, _saves);
        Assert.False(guard.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation);
    }

    [Theory]
    [InlineData(".unsaved-changes-continue", false)]
    [InlineData(".unsaved-changes-discard", true)]
    public async Task ContinueRetainsDraftAndDiscardLeavesWithoutSaving(
        string selector,
        bool leaves
    )
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var guard = Guard();
        var navigation = await BeginNavigationAsync(guard, "/destination");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(selector)));
        Assert.Contains("当前页面有未保存的修改", dialogs.Markup);
        Assert.False(navigation.IsCompleted);
        await dialogs.Find(selector).ClickAsync(new());
        Assert.Equal(leaves, await navigation);
        Assert.Equal(
            leaves ? "http://localhost/destination" : TestNavigationManager.InitialUri,
            _navigation.Uri
        );
        Assert.Equal(0, _saves);
        Assert.True(guard.Instance.HasChanges);
    }

    [Fact]
    public async Task SaveWaitsForPersistenceBeforeLeaving()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var guard = Guard(save: () =>
        {
            _saves++;
            return completion.Task;
        });
        var navigation = await BeginNavigationAsync(guard, "/saved");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        guard.WaitForAssertion(() => Assert.Equal(1, _saves));
        Assert.False(navigation.IsCompleted);
        Assert.Equal(TestNavigationManager.InitialUri, _navigation.Uri);
        await guard.InvokeAsync(() => completion.SetResult(true));
        Assert.True(await navigation);
        Assert.Equal("http://localhost/saved", _navigation.Uri);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task NavigationDuringSaveStaysOnPageWithoutAnotherPromptOrSave()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var guard = Guard();
        guard.SetParametersAndRender(p => p.Add(x => x.Busy, true));
        Assert.False(await await BeginNavigationAsync(guard, "/while-saving"));
        Assert.Equal(TestNavigationManager.InitialUri, _navigation.Uri);
        Assert.Empty(dialogs.FindAll(".unsaved-changes-save"));
        Assert.True(guard.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation);
        Assert.Equal(0, _saves);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSaveOrExceptionRetainsPageAndDoesNotExposeDetails(bool throws)
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var snackbars = RenderComponent<MudSnackbarProvider>();
        var guard = Guard(save: () =>
        {
            _saves++;
            return throws
                ? Task.FromException<bool>(new IOException("synthetic private failure"))
                : Task.FromResult(false);
        });
        var navigation = await BeginNavigationAsync(guard, "/failed");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.False(await navigation);
        Assert.Equal(TestNavigationManager.InitialUri, _navigation.Uri);
        Assert.True(guard.Instance.HasChanges);
        Assert.Equal(1, _saves);
        snackbars.WaitForAssertion(() => Assert.Contains("保存未完成", snackbars.Markup));
        Assert.DoesNotContain("synthetic private failure", snackbars.Markup);
    }

    [Fact]
    public async Task RepeatedNavigationSharesOneDialogAndSavesOnceForLatestDestination()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var guard = Guard();
        var first = await BeginNavigationAsync(guard, "/first");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        var latest = await BeginNavigationAsync(guard, "/latest");
        Assert.Single(dialogs.FindAll(".unsaved-changes-save"));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.False(await first);
        Assert.True(await latest);
        Assert.Equal("http://localhost/latest", _navigation.Uri);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task UnavailableSaveStillOffersDiscardAndContinue()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var guard = Guard();
        guard.SetParametersAndRender(p => p.Add(x => x.CanSave, false));
        var navigation = await BeginNavigationAsync(guard, "/unavailable");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        Assert.True(dialogs.Find(".unsaved-changes-save").HasAttribute("disabled"));
        Assert.Contains("当前操作完成后可以保存修改", dialogs.Markup);
        await dialogs.Find(".unsaved-changes-continue").ClickAsync(new());
        Assert.False(await navigation);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task ValidationPreventsSavingAndLeavingUntilFieldsAreValid()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var model = new RequiredSettings();
        var form = RenderComponent<EditForm>(p =>
            p.Add(x => x.Model, model)
                .Add(
                    x => x.ChildContent,
                    _ =>
                        builder =>
                        {
                            builder.OpenComponent<DataAnnotationsValidator>(0);
                            builder.CloseComponent();
                            builder.OpenComponent<UnsavedChangesGuard>(1);
                            builder.AddAttribute(2, nameof(UnsavedChangesGuard.HasChanges), true);
                            builder.AddAttribute(
                                3,
                                nameof(UnsavedChangesGuard.SaveBeforeLeaving),
                                (Func<Task<bool>>)(
                                    () =>
                                    {
                                        _saves++;
                                        return Task.FromResult(true);
                                    }
                                )
                            );
                            builder.CloseComponent();
                        }
                )
        );
        var guard = form.FindComponent<UnsavedChangesGuard>();
        var invalid = await BeginNavigationAsync(guard, "/valid");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.False(await invalid);
        Assert.Equal(0, _saves);
        model.Name = "示例设置";
        var valid = await BeginNavigationAsync(guard, "/valid");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.True(await valid);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task DisposingPageClosesPromptAndDoesNotSave()
    {
        var dialogs = RenderComponent<MudDialogProvider>();
        var guard = Guard();
        var navigation = await BeginNavigationAsync(guard, "/disposed");
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await guard.InvokeAsync(guard.Instance.Dispose);
        Assert.False(await navigation);
        dialogs.WaitForAssertion(() => Assert.Empty(dialogs.FindAll(".unsaved-changes-save")));
        Assert.Equal(0, _saves);
    }

    public sealed class RequiredSettings
    {
        [Required]
        public string Name { get; set; } = "";
    }

    internal sealed class TestNavigationManager : NavigationManager
    {
        public const string InitialUri = "http://localhost/Configurations/example";

        public TestNavigationManager() => Initialize("http://localhost/", InitialUri);

        protected override void SetNavigationLockState(bool value) { }

        protected override void NavigateToCore(string uri, bool forceLoad) =>
            throw new NotSupportedException();

        public async Task<bool> NavigateAsync(string target)
        {
            var uri = ToAbsoluteUri(target).AbsoluteUri;
            if (!await NotifyLocationChangingAsync(uri, null, false))
                return false;
            Uri = uri;
            NotifyLocationChanged(false);
            return true;
        }
    }
}
