using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Web.Components.Pages.Today;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class TodayAccountSwitchingTests : TestContext
{
    private readonly AccountProxy _service;

    public TodayAccountSwitchingTests()
    {
        Services.AddMudServices();
        Services.Configure<AutoRecoverOptions>(_ => { });
        JSInterop.Mode = JSRuntimeMode.Loose;
        var service = DispatchProxy.Create<ITodayTaskService, AccountProxy>();
        _service = (AccountProxy)service;
        Services.AddSingleton(service);
    }

    [Fact]
    public void ArrowsSwitchAndWrapWithoutReloadingAccountStatuses()
    {
        var page = RenderComponent<Today>();
        AssertAccount(page, "示例账号甲", "0 / 1");
        page.Find("button[aria-label='下一个账号']").Click();
        AssertAccount(page, "示例账号乙", "1 / 1");
        page.Find("button[aria-label='下一个账号']").Click();
        AssertAccount(page, "示例账号甲", "0 / 1");
        page.Find("button[aria-label='上一个账号']").Click();
        AssertAccount(page, "示例账号乙", "1 / 1");
        Assert.Equal(2, _service.StatusRequests);
    }

    [Fact]
    public async Task DropdownCanShowAllAccountsAndArrowsReturnToSingleAccount()
    {
        var page = RenderComponent<Today>();
        await SelectAccount(page, 1002);
        AssertAccount(page, "示例账号乙", "1 / 1");
        await SelectAccount(page, 0);
        Assert.Equal(2, page.FindAll(".today-account-card").Count);
        Assert.Contains("补做全部账号漏做项", page.Markup);
        page.Find("button[aria-label='上一个账号']").Click();
        AssertAccount(page, "示例账号乙", "1 / 1");
        await SelectAccount(page, 0);
        page.Find("button[aria-label='下一个账号']").Click();
        AssertAccount(page, "示例账号甲", "0 / 1");
    }

    [Fact]
    public void RefreshKeepsTheSelectedUidWhenAccountOrderChanges()
    {
        var page = RenderComponent<Today>();
        page.Find("button[aria-label='下一个账号']").Click();
        _service.Accounts =
        [
            Account(1002, "更新后的账号乙", 0, true),
            Account(1001, "示例账号甲", 1),
        ];
        Button(page, "立即刷新").Click();
        AssertAccount(page, "更新后的账号乙", "1 / 1");
        Assert.Equal(3, _service.StatusRequests);
    }

    [Fact]
    public void RemovingTheSelectedAccountFallsBackAndDisablesArrowsForOneAccount()
    {
        var page = RenderComponent<Today>();
        page.Find("button[aria-label='下一个账号']").Click();
        _service.Accounts = [Account(1001, "示例账号甲", 0)];
        Button(page, "立即刷新").Click();
        AssertAccount(page, "示例账号甲", "0 / 1");
        Assert.True(page.Find("button[aria-label='上一个账号']").HasAttribute("disabled"));
        Assert.True(page.Find("button[aria-label='下一个账号']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task BackgroundRefreshPreservesAnAccountChosenWhileLoading()
    {
        var pending = new TaskCompletionSource<List<AccountTodayTasksDto>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.BiliResult = pending.Task;
        var page = RenderComponent<Today>();
        page.Find("button[aria-label='下一个账号']").Click();
        AssertAccount(page, "示例账号乙", "1 / 1");
        await page.InvokeAsync(() =>
            pending.SetResult([
                Account(1002, "最新账号乙", 0, true),
                Account(1001, "示例账号甲", 1),
            ])
        );
        page.WaitForAssertion(() => AssertAccount(page, "最新账号乙", "1 / 1"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TopRecoveryButtonUsesTheVisibleAccountScope(bool allAccounts)
    {
        var page = RenderComponent<Today>();
        await SelectAccount(page, allAccounts ? 0 : 1002);
        Button(page, allAccounts ? "补做全部账号漏做项" : "补做当前账号漏做项").Click();
        Assert.Equal(allAccounts ? new[] { 0L } : new[] { 1002L }, _service.RecoveredAccounts);
        Assert.Equal(allAccounts ? 2 : 1, page.FindAll(".today-account-card").Count);
        if (!allAccounts)
            AssertAccount(page, "示例账号乙", "1 / 1");
    }

    [Fact]
    public void RowRecoveryTargetsTheSelectedAccount()
    {
        _service.Accounts = [Account(1001, "示例账号甲", 0), Account(1002, "示例账号乙", 1)];
        var page = RenderComponent<Today>();
        page.Find("button[aria-label='下一个账号']").Click();
        page.Find(".today-task-row button").Click();
        Assert.Equal(new[] { 1002L }, _service.RecoveredAccounts);
        AssertAccount(page, "示例账号乙", "0 / 1");
    }

    [Fact]
    public void EmptyAccountListHasNoSelectorAndCannotRecover()
    {
        _service.Accounts = [];
        var page = RenderComponent<Today>();
        Assert.Empty(page.FindAll(".today-account-switcher"));
        Assert.Empty(page.FindAll(".today-account-card"));
        Assert.True(Button(page, "补做当前账号漏做项").HasAttribute("disabled"));
    }

    [Fact]
    public async Task RecoverySettingsSaveOnlyChangesAndShowPendingSuccessAndRetry()
    {
        var page = RenderComponent<Today>();
        var button = page.Find(".save-changes-button");
        Assert.True(button.HasAttribute("disabled"));
        var toggle = page.FindComponents<MudSwitch<bool>>()
            .Single(c => c.Instance.Label == "自动补做");
        var original = toggle.Find("input").HasAttribute("checked");
        toggle.Find("input").Change(!original);
        Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"));
        toggle.Find("input").Change(original);
        Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
        toggle.Find("input").Change(!original);
        _service.SettingsWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var saving = page.Find(".save-changes-button").ClickAsync(new());
        page.WaitForAssertion(() => Assert.Contains("正在保存", page.Markup));
        Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
        Assert.Equal(1, _service.SettingsWrites);
        await page.InvokeAsync(() => _service.SettingsWrite.SetResult());
        await saving;
        Assert.Contains("已保存", page.Find(".save-changes-state").TextContent);
        Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
        toggle.Find("input").Change(original);
        _service.SettingsWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        saving = page.Find(".save-changes-button").ClickAsync(new());
        await page.InvokeAsync(() =>
            _service.SettingsWrite.SetException(new IOException("synthetic private details"))
        );
        await saving;
        Assert.Contains("保存失败，修改已保留，请重试", page.Markup);
        Assert.DoesNotContain("synthetic private details", page.Markup);
        Assert.False(page.Find(".save-changes-button").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("item")]
    [InlineData("account")]
    [InlineData("all")]
    public async Task RecoveryShowsScopedSpinnerThenRefreshPhaseAndPersistentResult(string scope)
    {
        var page = RenderComponent<Today>();
        if (scope == "all")
            await SelectAccount(page, 0);
        var redo = new TaskCompletionSource<TaskRedoResultDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var batch = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.RedoResult = redo.Task;
        _service.BatchResult = batch.Task;
        var selector =
            scope == "item" ? ".today-redo-item"
            : scope == "account" ? ".today-redo-account"
            : ".today-redo-visible";
        var click = page.Find(selector).ClickAsync(new());
        page.WaitForAssertion(() =>
            Assert.Contains("正在补做", page.Find(".today-redo-feedback").TextContent)
        );
        Assert.Equal("true", page.Find(selector).GetAttribute("aria-busy"));
        Assert.Equal("-1", page.Find(".today-recovery-panel").GetAttribute("tabindex"));
        Assert.Single(JSInterop.Invocations["biliTool.focusRecoveryProgress"]);
        Assert.NotEmpty(page.Find(selector).QuerySelectorAll(".mud-progress-circular"));
        Assert.Contains("补做中", page.Find(selector).TextContent);
        Assert.All(
            page.FindAll(".today-redo-item"),
            button => Assert.True(button.HasAttribute("disabled"))
        );
        if (scope == "item")
            Assert.Contains("正在补做", page.Find(".today-redo-row-feedback").TextContent);
        else
        {
            Assert.NotEmpty(page.FindAll(".today-redo-account-feedback"));
            Assert.Empty(page.FindAll(".today-redo-item[aria-busy=true]"));
        }
        await page.Find(selector).ClickAsync(new());
        Assert.Single(_service.RecoveredAccounts);
        var refreshed = new TaskCompletionSource<List<AccountTodayTasksDto>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.BiliResult = refreshed.Task;
        await page.InvokeAsync(() =>
        {
            redo.SetResult(new(true, "示例补做结果"));
            batch.SetResult(2);
        });
        page.WaitForAssertion(() =>
            Assert.Contains("正在更新任务状态", page.Find(".today-redo-feedback").TextContent)
        );
        Assert.Contains("更新中", page.Find(selector).TextContent);
        Assert.True(page.Find(selector).HasAttribute("disabled"));
        await page.InvokeAsync(() =>
            refreshed.SetResult([
                Account(1001, "示例账号甲", 0, true),
                Account(1002, "示例账号乙", 1, true),
            ])
        );
        await click;
        Assert.Contains(
            scope == "item" ? "示例补做结果" : "共执行 2 项",
            page.Find(".today-redo-feedback").TextContent
        );
        Assert.DoesNotContain("正在补做", page.Find(".today-redo-feedback").TextContent);
        Assert.Empty(page.FindAll("button[aria-busy=true]"));
        if (scope == "item")
        {
            Assert.Contains("示例补做结果", page.Find(".today-redo-row-feedback").TextContent);
            Assert.Empty(page.FindAll(".today-redo-item"));
        }
        Assert.False(Button(page, "立即刷新").HasAttribute("disabled"));
        Assert.Single(JSInterop.Invocations["biliTool.focusRecoveryProgress"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecoveryFailureKeepsVisibleResultAndAllowsRetryWithoutBreakingPage(
        bool throws
    )
    {
        var page = RenderComponent<Today>();
        _service.RedoResult = throws
            ? Task.FromException<TaskRedoResultDto>(new IOException("synthetic private details"))
            : Task.FromResult(new TaskRedoResultDto(false, "示例任务执行失败"));
        await page.Find(".today-redo-item").ClickAsync(new());
        var expected = throws ? "读取或保存任务数据失败，请查看执行记录" : "示例任务执行失败";
        Assert.Contains(expected, page.Find(".today-redo-feedback").TextContent);
        Assert.Contains(expected, page.Find(".today-redo-row-feedback").TextContent);
        Assert.DoesNotContain("synthetic private details", page.Markup);
        Assert.False(page.Find(".today-redo-item").HasAttribute("disabled"));
        _service.RedoResult = Task.FromResult(new TaskRedoResultDto(true, "重新执行已结束"));
        await page.Find(".today-redo-item").ClickAsync(new());
        Assert.Contains("重新执行已结束", page.Find(".today-redo-feedback").TextContent);
        Assert.Equal(2, _service.RecoveredAccounts.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task BatchResultReportsExecutedCountAndNoWorkWithoutClaimingAllTasksCompleted(
        int count
    )
    {
        var page = RenderComponent<Today>();
        _service.BatchResult = Task.FromResult(count);
        await page.Find(".today-redo-visible").ClickAsync(new());
        Assert.Contains(
            count == 0 ? "当前没有需要补做的任务" : "本轮补做已结束，共执行 2 项",
            page.Find(".today-redo-feedback").TextContent
        );
        Assert.Contains("未完成", page.Find(".today-account-card").TextContent);
        Assert.DoesNotContain("已补做 2 项", page.Markup);
    }

    [Fact]
    public async Task RecoveryRefreshFailureKeepsExecutionResultAndReleasesButtons()
    {
        var page = RenderComponent<Today>();
        _service.BiliResult = Task.FromException<List<AccountTodayTasksDto>>(
            new IOException("synthetic private refresh details")
        );
        await page.Find(".today-redo-item").ClickAsync(new());
        Assert.Contains("示例补做结果", page.Find(".today-redo-feedback").TextContent);
        Assert.Contains("任务状态更新未完成", page.Find(".today-redo-row-feedback").TextContent);
        Assert.DoesNotContain("synthetic private refresh details", page.Markup);
        Assert.False(page.Find(".today-redo-item").HasAttribute("disabled"));
        Assert.False(Button(page, "立即刷新").HasAttribute("disabled"));
        Assert.Single(JSInterop.Invocations["biliTool.focusRecoveryProgress"]);
    }

    [Fact]
    public async Task LateInitialRefreshCannotOverwriteStatusRefreshedAfterRecovery()
    {
        var initial = new TaskCompletionSource<List<AccountTodayTasksDto>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _service.BiliResult = initial.Task;
        var page = RenderComponent<Today>();
        page.WaitForAssertion(() => Assert.Equal(2, _service.StatusRequests));
        _service.BiliResult = Task.FromResult(
            new List<AccountTodayTasksDto> { Account(1001, "示例账号甲", 0, true) }
        );
        await page.Find(".today-redo-item").ClickAsync(new());
        AssertAccount(page, "示例账号甲", "1 / 1");
        await page.InvokeAsync(() => initial.SetResult([Account(1001, "旧状态账号甲", 0)]));
        AssertAccount(page, "示例账号甲", "1 / 1");
        Assert.DoesNotContain("旧状态账号甲", page.Markup);
    }

    private static Task SelectAccount(IRenderedComponent<Today> page, long userId) =>
        page.InvokeAsync(() =>
            page.FindComponent<MudSelect<long>>().Instance.ValueChanged.InvokeAsync(userId)
        );

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<Today> page, string text) =>
        page.FindAll("button").Single(button => button.TextContent.Trim() == text);

    private static void AssertAccount(IRenderedComponent<Today> page, string name, string progress)
    {
        var card = Assert.Single(page.FindAll(".today-account-card"));
        Assert.Contains(name, card.TextContent);
        Assert.Contains("已完成 " + progress, card.TextContent);
    }

    public static AccountTodayTasksDto Account(
        long uid,
        string name,
        int index,
        bool completed = false
    ) =>
        new()
        {
            UserId = uid,
            UserName = name,
            Index = index,
            IsCookieValid = true,
            Groups =
            [
                new()
                {
                    TaskKey = "VipPrivilegeTaskAppService",
                    DisplayName = "大会员福利",
                    Items =
                    [
                        new()
                        {
                            DisplayName = "大会员福利",
                            State = completed
                                ? TodayTaskItemState.Completed
                                : TodayTaskItemState.NotDone,
                            StateText = completed ? "已完成" : "未完成",
                        },
                    ],
                },
            ],
        };

    [Fact]
    public async Task LeavingTodaySettingsSavesRecoveryDraftBeforeNavigation()
    {
        var navigation = new UnsavedChangesNavigationTests.TestNavigationManager();
        Services.AddSingleton<NavigationManager>(navigation);
        var dialogs = RenderComponent<MudDialogProvider>();
        var page = RenderComponent<Today>();
        var enabled = page.FindComponent<MudSwitch<bool>>();
        enabled.Find("input").Change(!enabled.Instance.GetState(x => x.Value));
        Task<bool>? leaving = null;
        await page.InvokeAsync(() =>
        {
            leaving = navigation.NavigateAsync("/BiliAccount");
        });
        dialogs.WaitForAssertion(() => Assert.Single(dialogs.FindAll(".unsaved-changes-save")));
        await dialogs.Find(".unsaved-changes-save").ClickAsync(new());
        Assert.True(await leaving!);
        Assert.Equal(1, _service.SettingsWrites);
        Assert.Equal("http://localhost/BiliAccount", navigation.Uri);
        Assert.True(page.Find(".save-changes-button").HasAttribute("disabled"));
    }

    [Fact]
    public async Task RecoveryProgressRendersDuringExecutionAndKeepsSpecificFailureAfterRefresh()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.RecoveryAction = async () =>
        {
            using var account = new TaskRecoveryProgressScope("1001/medal", 1001);
            TaskRecoveryProgressScope.Report(
                "watch",
                "示例牌 · 观看直播",
                TaskRecoveryProgressState.Waiting,
                "观看会话保持中，30 秒后发送下次心跳",
                60,
                900,
                "秒",
                "观看15分钟 · 每日上限 0/10 · 未完成"
            );
            await release.Task;
            TaskRecoveryProgressScope.Report(
                "watch",
                "示例牌 · 观看直播",
                TaskRecoveryProgressState.Failed,
                "心跳被拒绝，错误码 1012002。重试 2 次后停止"
            );
            return new(false, "有粉丝牌动作失败，请查看补做进度");
        };
        var page = RenderComponent<Today>();
        var click = page.Find(".today-redo-item").ClickAsync(new());
        page.WaitForAssertion(() =>
        {
            Assert.Contains("本次心跳 1 分钟 / 计划 15 分钟", page.Markup);
            Assert.Contains("B 站进度", page.Markup);
            Assert.Contains("0/10", page.Markup);
            Assert.Contains("等待中", page.Markup);
            Assert.Contains("示例账号甲", page.Find(".recovery-panel").TextContent);
        });
        await page.InvokeAsync(() => release.SetResult());
        await click;
        Assert.Contains("1012002", page.Find(".recovery-panel").TextContent);
        Assert.Contains("重试 2 次", page.Find(".recovery-panel").TextContent);
        Assert.Equal("Failed", page.Find(".recovery-entry").GetAttribute("data-state"));
        Assert.False(page.Find(".today-redo-item").HasAttribute("disabled"));
    }

    [Fact]
    public void FailedRecoveryRowsAppearBeforeCompletedRows()
    {
        var panel = RenderComponent<Ray.BiliBiliTool.Web.Components.Comps.RecoveryProgressPanel>(
            parameters =>
                parameters.Add(
                    component => component.Entries,
                    new TaskRecoveryProgress[]
                    {
                        new("first", "已完成的点赞", TaskRecoveryProgressState.Completed),
                        new(
                            "failure",
                            "失败的观看",
                            TaskRecoveryProgressState.Failed,
                            "错误码 1012002"
                        ),
                        new("third", "等待中的弹幕", TaskRecoveryProgressState.Pending),
                        new(
                            "1001/medal/60/watchLive",
                            "主播观看失败",
                            TaskRecoveryProgressState.Failed,
                            "该主播心跳拒绝"
                        ),
                    }
                )
        );
        var rows = panel.FindAll(".recovery-entry");
        Assert.Contains("该主播心跳拒绝", rows[0].TextContent);
        Assert.Contains("1012002", rows[1].TextContent);
        Assert.Contains("等待中的弹幕", rows[2].TextContent);
        Assert.Contains("已完成的点赞", rows[3].TextContent);
    }

    [Fact]
    public void RecoveryCompletionMovesRowsAfterPendingRowsDuringExecution()
    {
        var panel = RenderComponent<Ray.BiliBiliTool.Web.Components.Comps.RecoveryProgressPanel>(
            parameters =>
                parameters.Add(
                    component => component.Entries,
                    new TaskRecoveryProgress[]
                    {
                        new("a", "点赞", TaskRecoveryProgressState.Running),
                        new("b", "观看", TaskRecoveryProgressState.Pending),
                        new("c", "已跳过", TaskRecoveryProgressState.Skipped),
                    }
                )
        );
        Assert.Equal("a", panel.FindAll(".recovery-entry")[0].GetAttribute("data-progress-key"));
        panel.SetParametersAndRender(parameters =>
            parameters.Add(
                component => component.Entries,
                new TaskRecoveryProgress[]
                {
                    new("a", "点赞", TaskRecoveryProgressState.Completed),
                    new("b", "观看", TaskRecoveryProgressState.Pending),
                    new("c", "已跳过", TaskRecoveryProgressState.Skipped),
                }
            )
        );
        Assert.Equal(
            new[] { "b", "a", "c" },
            panel.FindAll(".recovery-entry").Select(row => row.GetAttribute("data-progress-key"))
        );
    }

    [Fact]
    public async Task TodayTaskGroupMovesCompletedItemsBackAfterRecoveryAndRemovesTheirButtons()
    {
        TodayTaskItemDto Item(string key, bool done) =>
            new()
            {
                ItemKey = key,
                DisplayName = key,
                State = done ? TodayTaskItemState.Completed : TodayTaskItemState.NotDone,
                StateText = done ? "已完成" : "未完成",
            };
        var account = Account(1001, "示例账号甲", 0);
        account.Groups.Clear();
        account.Groups.Add(
            new()
            {
                TaskKey = "DailyTaskAppService",
                DisplayName = "每日任务",
                Items = [Item("Login", true), Item("Watch", false), Item("Share", false)],
            }
        );
        _service.Accounts = [account];
        var page = RenderComponent<Today>();
        Assert.Equal(
            new[] { "Watch", "Share", "Login" },
            page.FindAll(".today-task-heading > p:first-child").Select(row => row.TextContent)
        );
        account.Groups[0].Items[1] = Item("Watch", true);
        await page.FindAll(".today-redo-item")[0].ClickAsync(new());
        Assert.Equal(
            new[] { "Share", "Login", "Watch" },
            page.FindAll(".today-task-heading > p:first-child").Select(row => row.TextContent)
        );
        Assert.Single(page.FindAll(".today-redo-item"));
        Assert.Contains(
            "Share",
            page.Find(".today-redo-item").Closest(".today-task-row")!.TextContent
        );
    }

    public class AccountProxy : DispatchProxy
    {
        public List<AccountTodayTasksDto> Accounts { get; set; } =
        [Account(1001, "示例账号甲", 0), Account(1002, "示例账号乙", 1, true)];
        public Task<List<AccountTodayTasksDto>>? BiliResult { get; set; }
        public Task<TaskRedoResultDto>? RedoResult { get; set; }
        public Task<int>? BatchResult { get; set; }
        public Func<Task<TaskRedoResultDto>>? RecoveryAction { get; set; }
        public int StatusRequests { get; private set; }
        public int SettingsWrites { get; private set; }
        public TaskCompletionSource? SettingsWrite { get; set; }
        public List<long> RecoveredAccounts { get; } = [];

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method!.Name)
            {
                case "SaveAutoRecoverSettingsAsync":
                    SettingsWrites++;
                    return SettingsWrite?.Task ?? Task.CompletedTask;
                case "GetTodayStatusAsync":
                    StatusRequests++;
                    return (bool)args![0]! && BiliResult is not null
                        ? BiliResult
                        : Task.FromResult(Accounts);
                case "RedoAllForAccountAsync":
                    RecoveredAccounts.Add((long)args![0]!);
                    return BatchResult ?? Task.FromResult(0);
                case "RedoAllMissingAsync":
                    RecoveredAccounts.Add(0);
                    return BatchResult ?? Task.FromResult(0);
                case "RedoAsync":
                    RecoveredAccounts.Add((long)args![0]!);
                    return RecoveryAction?.Invoke()
                        ?? RedoResult
                        ?? Task.FromResult(new TaskRedoResultDto(true, "示例补做结果"));
                default:
                    throw new InvalidOperationException(method.Name);
            }
        }
    }
}
