using Bunit;
using Microsoft.Extensions.DependencyInjection;
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
        page.Find("input[type=password]").Change("SCT123synthetic");
        page.Find("button").Click();
        Assert.Equal("SCT123synthetic", _workflow.NewKey);
        Assert.Contains("设置已保存。SendKey 已配置。", page.Find("[role=status]").TextContent);
        Assert.Equal("", page.Find("input[type=password]").GetAttribute("value") ?? "");
        Assert.Equal("********", page.Find("input[type=password]").GetAttribute("placeholder"));
        Assert.DoesNotContain("SCT123synthetic", page.Markup);
        page.Find("button").Click();
        Assert.Equal("", _workflow.NewKey);
    }

    [Fact]
    public void EditingReplacementKey_ClearsOldSuccessFeedbackAndShowsUnsavedState()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.Find("button").Click();
        Assert.NotEmpty(page.FindAll("[role=status]"));
        page.Find("input[type=password]").Change("SCT123synthetic");
        Assert.Empty(page.FindAll("[role=status]"));
        Assert.Contains("新密钥待保存", page.Markup);
    }

    [Fact]
    public void AutomaticSwitch_CanBeSavedAsManualMode()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        page.FindAll("input[type=checkbox]")[0].Change(false);
        page.Find("button").Click();
        Assert.False(_workflow.Automatic);
        Assert.Contains("手动模式", page.Markup);
    }

    [Fact]
    public void UnifiedPage_ShowsAllSettingsAndOneSaveAction_WithDirectCopy()
    {
        var page = RenderComponent<CookieNotificationConfig>();
        Assert.Contains("登录状态检查", page.Markup);
        Assert.Contains("任务失败汇总", page.Markup);
        Assert.Contains("后台定时任务结束后等待 2 分钟", page.Markup);
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
        page.Find("button").Click();
        Assert.Single(page.FindAll("[role=status]"));
        page.FindAll("input[type=checkbox]")[0].Change(false);
        Assert.Empty(page.FindAll("[role=status]"));
    }

    private sealed class FakeWorkflow : INotificationSettingsWorkflow
    {
        public string? NewKey { get; private set; }
        public bool Automatic { get; private set; } = true;

        public NotificationSettings Read() => new(new(true, true, true), new(true, true, []));

        public void Save(
            bool autoCheckEnabled,
            bool expiryNotifyEnabled,
            bool failureNotifyEnabled,
            string? newSendKey,
            IReadOnlyDictionary<string, bool> tasks
        )
        {
            Automatic = autoCheckEnabled;
            NewKey = newSendKey;
        }
    }
}
