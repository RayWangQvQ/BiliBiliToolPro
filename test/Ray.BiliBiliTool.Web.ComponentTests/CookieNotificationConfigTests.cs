using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Pages.Configs;
using Ray.BiliBiliTool.Web.Services.Pages.Configs;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class CookieNotificationConfigTests : TestContext
{
    private readonly FakeWorkflow _workflow = new();

    public CookieNotificationConfigTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<INotificationSettingsWorkflow>(_workflow);
    }

    [Fact]
    public void ConfiguredKey_RendersPersistentMaskWithoutReturningSecret()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        Assert.Contains("SendKey 已配置", page.Markup);
        Assert.Equal("", page.Find("input[type=password]").GetAttribute("value") ?? "");
        Assert.Equal("********", page.Find("input[type=password]").GetAttribute("placeholder"));
    }

    [Fact]
    public void Save_ReplacesKeyThenShowsMaskAndSuccessFeedback()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("input[type=password]").Input("SCT123synthetic");
        page.Find("button").Click();
        page.WaitForAssertion(() =>
            Assert.Contains("设置已保存。SendKey 已配置。", page.Find(".save-result").TextContent)
        );
        Assert.Contains("设置已保存。SendKey 已配置。", page.Find(".save-result").TextContent);
        Assert.Equal("", page.Find("input[type=password]").GetAttribute("value") ?? "");
        Assert.Equal("********", page.Find("input[type=password]").GetAttribute("placeholder"));
        Assert.DoesNotContain("SCT123synthetic", page.Markup);
        Assert.True(page.Find("button").HasAttribute("disabled"));
        Assert.Equal("SCT123synthetic", _workflow.NewKey);
    }

    [Fact]
    public void EditingReplacementKey_ClearsOldSuccessFeedbackAndShowsUnsavedState()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.FindAll("input[type=checkbox]")[0].Change(false);
        page.Find("button").Click();
        page.WaitForAssertion(() =>
        {
            Assert.Single(page.FindAll(".save-result"));
            Assert.False(page.Find("input[type=password]").HasAttribute("disabled"));
        });
        page.Find("input[type=password]").Input("SCT123synthetic");
        page.WaitForAssertion(() =>
        {
            Assert.Empty(page.FindAll(".save-result"));
            Assert.Contains("新密钥待保存", page.Markup);
        });
    }

    [Fact]
    public void AutomaticSwitch_CanBeSavedAsManualMode()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.FindAll("input[type=checkbox]")[0].Change(false);
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.False(_workflow.Automatic));
        Assert.Contains("手动模式", page.Markup);
    }

    [Fact]
    public void UnifiedPage_ShowsAllSettingsAndOneSaveAction_WithDirectCopy()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        Assert.Contains("登录状态检查", page.Markup);
        Assert.Contains("每日任务汇总", page.Markup);
        Assert.Contains("当天自动任务全部结束后统一发送一次", page.Markup);
        Assert.DoesNotContain("手动补做和自动补做也会计入", page.Markup);
        Assert.Single(page.FindAll("input[type=password]"));
        Assert.Single(page.FindAll("button"));
        Assert.DoesNotContain("；", page.Markup);
        Assert.DoesNotContain("网络故障", page.Markup);
    }

    [Fact]
    public void EditingSwitch_ClearsPreviousSaveFeedback()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("input[type=password]").Input("SCT123synthetic");
        page.Find("button").Click();
        page.WaitForAssertion(() =>
        {
            Assert.Single(page.FindAll(".save-result"));
            Assert.False(page.FindAll("input[type=checkbox]")[0].HasAttribute("disabled"));
        });
        page.FindAll("input[type=checkbox]")[0].Change(false);
        page.WaitForAssertion(() =>
        {
            Assert.Empty(page.FindAll(".save-result"));
            Assert.False(page.Find("button").HasAttribute("disabled"));
            Assert.Contains("手动模式", page.Markup);
        });
    }

    [Fact]
    public void NotificationSaveEnablesForSwitchOrKeyAndDisablesOnRevertAndSuccess()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        Assert.True(page.Find("button").HasAttribute("disabled"));
        page.FindAll("input[type=checkbox]")[0].Change(false);
        Assert.False(page.Find("button").HasAttribute("disabled"));
        page.FindAll("input[type=checkbox]")[0].Change(true);
        Assert.True(page.Find("button").HasAttribute("disabled"));
        page.Find("input[type=password]").Input("   ");
        Assert.True(page.Find("button").HasAttribute("disabled"));
        page.Find("input[type=password]").Input("SCT123synthetic");
        Assert.False(page.Find("button").HasAttribute("disabled"));
        page.Find("button").Click();
        page.WaitForAssertion(() =>
        {
            Assert.True(page.Find("button").HasAttribute("disabled"));
            Assert.Contains("已保存", page.Find(".save-changes-state").TextContent);
        });
    }

    [Fact]
    public void NotificationSaveFailurePreservesKeyAndAllowsRetry()
    {
        _workflow.FailSave = true;
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("input[type=password]").Input("SCT123synthetic");
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.Contains("保存失败，请稍后重试", page.Markup));
        Assert.Equal("SCT123synthetic", page.Find("input[type=password]").GetAttribute("value"));
        Assert.False(page.Find("button").HasAttribute("disabled"));
        _workflow.FailSave = false;
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("button").HasAttribute("disabled")));
    }

    [Fact]
    public void DailySummaryTimeSupportsDraftRevertSaveAndReload()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        Assert.Equal("23", page.Find("select[aria-label='汇总小时']").GetAttribute("value"));
        page.Find("select[aria-label='汇总小时']").Change("18");
        Assert.False(page.Find("button").HasAttribute("disabled"));
        page.Find("select[aria-label='汇总小时']").Change("23");
        Assert.True(page.Find("button").HasAttribute("disabled"));
        page.Find("select[aria-label='汇总小时']").Change("18");
        page.Find("select[aria-label='汇总分钟']").Change("30");
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.Equal("18:30", _workflow.SummaryTime));
        Assert.True(page.Find("button").HasAttribute("disabled"));
        var reopened = RenderComponent<CookieNotificationConfig>();
        Assert.Equal("18", reopened.Find("select[aria-label='汇总小时']").GetAttribute("value"));
        Assert.Equal("30", reopened.Find("select[aria-label='汇总分钟']").GetAttribute("value"));
    }

    [Fact]
    public void DailySummaryFailedSaveKeepsTimeDraftAndDisabledReminderBlocksPicker()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("select[aria-label='汇总小时']").Change("19");
        _workflow.FailSave = true;
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.Contains("保存失败", page.Markup));
        Assert.Equal("19", page.Find("select[aria-label='汇总小时']").GetAttribute("value"));
        Assert.Equal("23:55", _workflow.SummaryTime);
        Assert.False(page.Find("button").HasAttribute("disabled"));
        _workflow.FailSave = false;
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.Equal("19:55", _workflow.SummaryTime));
        page.FindAll("input[type=checkbox]")[2].Change(false);
        Assert.True(page.Find("fieldset.schedule-picker").HasAttribute("disabled"));
    }

    [Fact]
    public async Task LeavingNotificationSettingsRetainsFailedDraftThenSavesBeforeNavigation()
    {
        var navigation = new UnsavedChangesNavigationTests.TestNavigationManager();
        Services.AddSingleton<NavigationManager>(navigation);
        var dialogs = RenderComponent<MudDialogProvider>();
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("input[type=password]").Input("SCTsynthetic-unsaved");
        _workflow.FailSave = true;
        Task<bool>? leaving = null;
        await page.InvokeAsync(() =>
        {
            leaving = navigation.NavigateAsync("/Today");
        });
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        Assert.DoesNotContain("SCTsynthetic-unsaved", dialogs.Markup);
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.False(await leaving!);
        Assert.Equal(
            "SCTsynthetic-unsaved",
            page.Find("input[type=password]").GetAttribute("value")
        );
        Assert.Equal(
            UnsavedChangesNavigationTests.TestNavigationManager.InitialUri,
            navigation.Uri
        );
        _workflow.FailSave = false;
        await page.InvokeAsync(() =>
        {
            leaving = navigation.NavigateAsync("/Today");
        });
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.True(await leaving!);
        Assert.Equal("SCTsynthetic-unsaved", _workflow.NewKey);
        Assert.Equal("http://localhost/Today", navigation.Uri);
        Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
    }

    private sealed class FakeWorkflow : INotificationSettingsWorkflow
    {
        public bool FailSave { get; set; }
        public string? NewKey { get; private set; }
        public bool Automatic { get; private set; } = true;

        public string SummaryTime { get; private set; } = "23:55";

        public NotificationSettings Read() =>
            new(new(true, true, true), new(true, true, [], SummaryTime));

        public void Save(
            bool autoCheckEnabled,
            bool expiryNotifyEnabled,
            bool failureNotifyEnabled,
            string? newSendKey,
            IReadOnlyDictionary<string, bool> tasks,
            string? dailySummaryTime
        )
        {
            Save(autoCheckEnabled, expiryNotifyEnabled, failureNotifyEnabled, newSendKey, tasks);
            SummaryTime = dailySummaryTime!;
        }

        public void Save(
            bool autoCheckEnabled,
            bool expiryNotifyEnabled,
            bool failureNotifyEnabled,
            string? newSendKey,
            IReadOnlyDictionary<string, bool> tasks
        )
        {
            if (FailSave)
                throw new IOException("synthetic failure");
            Automatic = autoCheckEnabled;
            NewKey = newSendKey;
        }
    }
}
