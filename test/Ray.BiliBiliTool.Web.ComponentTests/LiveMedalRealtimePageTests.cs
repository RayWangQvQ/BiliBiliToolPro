using BlazingQuartz.Core.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Web.Components.Comps;
using Ray.BiliBiliTool.Web.Components.Pages.Configs;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class LiveMedalRealtimePageTests : TestContext
{
    private sealed class Dashboard : ILiveMedalDashboardService
    {
        public TimeSpan AutoRefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
        public int Reads;
        public Func<int, int, CancellationToken, Task<LiveMedalSnapshot>>? Read;
        public Func<int, CancellationToken, Task<LiveMedalSnapshot?>>? Cached;
        public IReadOnlyList<LiveMedalAccount> Accounts =
        [
            new(0, "账号1", "synthetic-account-a"),
            new(1, "账号2", "synthetic-account-b"),
        ];
        public int CacheReads;
        public int FailNextAccountRead;
        public Dictionary<int, Action<LiveMedalSnapshot>> Listeners = [];

        public IReadOnlyList<LiveMedalAccount> GetAccounts()
        {
            if (Interlocked.Exchange(ref FailNextAccountRead, 0) != 0)
                throw new IOException("synthetic account lookup failure");
            return Accounts;
        }

        public Task<LiveMedalSnapshot?> GetCachedAsync(int index, CancellationToken token = default)
        {
            Interlocked.Increment(ref CacheReads);
            return Cached?.Invoke(index, token) ?? Task.FromResult<LiveMedalSnapshot?>(null);
        }

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        )
        {
            Assert.True(refresh);
            var count = Interlocked.Increment(ref Reads);
            return Read?.Invoke(index, count, token) ?? Task.FromResult(Snapshot(index));
        }

        public IDisposable Subscribe(int index, Action<LiveMedalSnapshot> listener)
        {
            Listeners[index] = listener;
            return new Subscription(() => Listeners.Remove(index));
        }

        private sealed class Subscription(Action remove) : IDisposable
        {
            public void Dispose() => remove();
        }
    }

    public LiveMedalRealtimePageTests()
    {
        Services.AddLogging();
        Services.AddMudServices();
        Services.AddQuartz();
        Services.AddSingleton<ISchedulerService, SchedulerService>();
        Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection().Build()
        );
        Services.AddSingleton<ILiveMedalParticipationWorkflow>(
            provider => new LiveMedalParticipationWorkflow(
                provider.GetRequiredService<IConfiguration>()
            )
        );
        Services.Configure<LiveFansMedalTaskOptions>(options => options.IsEnable = true);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static LiveMedalSnapshot Snapshot(
        int account = 0,
        int current = 0,
        long revision = 1
    ) =>
        new(
            Enumerable
                .Range(1, 14)
                .Select(id => new LiveMedalCard(
                    id,
                    $"账号{account + 1}主播{id}",
                    "示例牌",
                    30 - id,
                    true,
                    true,
                    false,
                    [
                        new(
                            "like",
                            "点赞30次",
                            $"每日上限 {current}/10",
                            current == 10,
                            current * 10
                        ),
                    ],
                    null,
                    RoomId: id,
                    ProgressUpdatedAt: DateTimeOffset.UtcNow
                ))
                .ToArray(),
            DateTimeOffset.UtcNow,
            Revision: revision
        );

    [Fact]
    public async Task LiveUpdatesChangeProgressWithoutReloadAndPreserveDraftSearchAndPagination()
    {
        var dashboard = new Dashboard();
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Equal(6, page.FindAll("article").Count));
        Assert.Contains("进度自动更新", page.Markup);
        page.FindComponents<MudSwitch<bool>>()
            .Single(item => item.Instance.Label == "仅在主播未开播时发弹幕")
            .Find("input")
            .Change(true);
        page.Find("input[type=search]").Input("主播");
        await page.InvokeAsync(() =>
            page.FindComponent<MudPagination>().Instance.SelectedChanged.InvokeAsync(2)
        );
        await page.InvokeAsync(() => dashboard.Listeners[0](Snapshot(current: 10, revision: 2)));
        page.WaitForAssertion(() =>
            Assert.Contains("已完成", page.Find("article[data-anchor='7']").TextContent)
        );
        Assert.Equal(1, dashboard.Reads);
        Assert.Equal("主播", page.Find("input[type=search]").GetAttribute("value"));
        Assert.Contains("第 2 / 3 页", page.Markup);
        Assert.True(
            page.FindComponents<MudSwitch<bool>>()
                .Single(item => item.Instance.Label == "仅在主播未开播时发弹幕")
                .Instance.Value
        );
        Assert.False(
            page.FindAll("button")
                .Single(button => button.ClassList.Contains("save-changes-button"))
                .HasAttribute("disabled")
        );
    }

    [Fact]
    public async Task SwitchingAccountsDropsPreviousSubscriptionAndLateEvents()
    {
        var dashboard = new Dashboard();
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Single(dashboard.Listeners));
        var previous = dashboard.Listeners[0];
        await page.Find("select[aria-label='查看账号']")
            .ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "1" });
        Assert.False(dashboard.Listeners.ContainsKey(0));
        await page.InvokeAsync(() => previous(Snapshot(0, 10, 99)));
        Assert.Contains("账号2主播", page.Markup);
        Assert.DoesNotContain("每日上限 10/10", page.Markup);
        await page.InvokeAsync(() => dashboard.Listeners[1](Snapshot(1, 7, 2)));
        page.WaitForAssertion(() => Assert.Contains("每日上限 7/10", page.Markup));
    }

    [Fact]
    public async Task SilentRefreshRecoversFromFailureWithoutHidingCardsOrBlockingAccountSwitch()
    {
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(100) };
        var pending = new TaskCompletionSource<LiveMedalSnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dashboard.Read = (index, count, token) =>
        {
            if (count == 3)
                started.TrySetResult();
            return count switch
            {
                1 => Task.FromResult(Snapshot()),
                2 => Task.FromException<LiveMedalSnapshot>(
                    new IOException("synthetic private details")
                ),
                3 => pending.Task.WaitAsync(token),
                _ => Task.FromResult(Snapshot(index, 7, 4)),
            };
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("稍后自动重试", page.Markup));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, dashboard.Reads);
        Assert.Equal(6, page.FindAll("article").Count);
        Assert.DoesNotContain("正在更新今日进度", page.Markup);
        Assert.DoesNotContain("synthetic private", page.Markup);
        Assert.False(page.Find("select[aria-label='查看账号']").HasAttribute("disabled"));
        await page.Find("select[aria-label='查看账号']")
            .ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "1" });
        page.WaitForAssertion(() => Assert.Contains("账号2主播", page.Markup));
        Assert.Contains("每日上限 7/10", page.Markup);
        Assert.DoesNotContain("稍后自动重试", page.Markup);
        pending.TrySetResult(Snapshot(0, 10, 99));
        Assert.DoesNotContain("账号1主播", page.Markup);
    }

    private sealed class PageHost : ComponentBase
    {
        [Parameter]
        public bool Show { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (!Show)
                return;
            builder.OpenComponent<LiveFansMedalTaskConfig>(0);
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task DisposingPageStopsPollingAndRemovesProgressSubscription()
    {
        var polled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(50) };
        dashboard.Read = (index, count, token) =>
        {
            if (count >= 2)
                polled.TrySetResult();
            return Task.FromResult(Snapshot(index));
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var host = RenderComponent<PageHost>();
        await polled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var listener = dashboard.Listeners[0];
        await host.InvokeAsync(() =>
            host.SetParametersAndRender(parameters => parameters.Add(item => item.Show, false))
        );
        Assert.Empty(dashboard.Listeners);
        var reads = dashboard.Reads;
        listener(Snapshot(current: 10));
        await Task.Delay(150);
        Assert.Equal(reads, dashboard.Reads);
    }

    [Fact]
    public async Task LateFullRefreshCannotReplaceNewerPushedProgress()
    {
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(100) };
        var pending = new TaskCompletionSource<LiveMedalSnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dashboard.Read = (index, count, token) =>
        {
            if (count == 1)
                return Task.FromResult(Snapshot());
            started.TrySetResult();
            return pending.Task.WaitAsync(token);
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, dashboard.Reads);
        await page.InvokeAsync(() => dashboard.Listeners[0](Snapshot(current: 10, revision: 10)));
        await page.InvokeAsync(() => pending.SetResult(Snapshot(current: 3, revision: 9)));
        page.WaitForAssertion(() => Assert.Contains("每日上限 10/10", page.Markup));
        Assert.DoesNotContain("每日上限 3/10", page.Markup);
    }

    [Fact]
    public async Task LateCachedReadCannotOverwritePushedProgressWhenFullRefreshFails()
    {
        var cached = new TaskCompletionSource<LiveMedalSnapshot?>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var dashboard = new Dashboard
        {
            Cached = (_, token) => cached.Task.WaitAsync(token),
            Read = (_, _, _) =>
                Task.FromException<LiveMedalSnapshot>(new IOException("synthetic refresh failure")),
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Equal(1, dashboard.CacheReads));
        await page.InvokeAsync(() => dashboard.Listeners[0](Snapshot(current: 10, revision: 10)));
        page.WaitForAssertion(() =>
            Assert.Equal(10, page.FindComponent<LiveMedalDashboard>().Instance.Snapshot!.Revision)
        );
        await page.InvokeAsync(() => cached.SetResult(Snapshot(current: 2, revision: 1)));
        page.WaitForAssertion(() => Assert.Contains("稍后自动重试", page.Markup));
        Assert.Contains("每日上限 10/10", page.Markup);
        Assert.DoesNotContain("每日上限 2/10", page.Markup);
    }

    [Fact]
    public async Task AddingAccountToInitiallyEmptyPageStartsLoadingWithoutReload()
    {
        var dashboard = new Dashboard
        {
            Accounts = [],
            AutoRefreshInterval = TimeSpan.FromMilliseconds(50),
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("添加 B 站账号后", page.Markup));
        await page.InvokeAsync(() => dashboard.Accounts = [new(0, "账号1", "synthetic-account-a")]);
        page.WaitForAssertion(
            () => Assert.Contains("账号1主播", page.Markup),
            TimeSpan.FromSeconds(5)
        );
        Assert.True(dashboard.Reads > 0);
        Assert.Single(dashboard.Listeners);
    }

    [Fact]
    public async Task ManualRefreshAlsoDiscoversAccountsAddedToEmptyPage()
    {
        var dashboard = new Dashboard { Accounts = [] };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        await page.InvokeAsync(() => dashboard.Accounts = [new(0, "账号1", "synthetic-account-a")]);
        await page.FindAll("button")
            .Single(button => button.TextContent.Contains("刷新进度"))
            .ClickAsync(new());
        page.WaitForAssertion(() => Assert.Contains("账号1主播", page.Markup));
    }

    [Fact]
    public async Task ReplacingAccountClearsOldCardsAndUsesNewAccountsCacheOnRefreshFailure()
    {
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(50) };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("账号1主播", page.Markup));
        var previousListener = dashboard.Listeners[0];
        await page.InvokeAsync(() =>
        {
            dashboard.Accounts = [new(0, "账号1", "synthetic-account-c")];
            dashboard.Cached = (_, _) =>
                Task.FromResult<LiveMedalSnapshot?>(Snapshot(account: 2, current: 7, revision: 2));
            dashboard.Read = (_, _, _) =>
                Task.FromException<LiveMedalSnapshot>(new IOException("synthetic refresh failure"));
        });
        page.WaitForAssertion(
            () => Assert.Contains("账号3主播", page.Markup),
            TimeSpan.FromSeconds(5)
        );
        await page.InvokeAsync(() => previousListener(Snapshot(current: 10, revision: 99)));
        Assert.DoesNotContain("账号1主播", page.Markup);
        Assert.Contains("每日上限 7/10", page.Markup);
        Assert.Equal(2, dashboard.CacheReads);
    }

    [Fact]
    public async Task RemovingLastAccountClearsCardsAndAddingAnotherAccountRecovers()
    {
        var dashboard = new Dashboard
        {
            Accounts = [new(0, "账号1", "synthetic-account-a")],
            AutoRefreshInterval = TimeSpan.FromMilliseconds(50),
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("账号1主播", page.Markup));
        await page.InvokeAsync(() => dashboard.Accounts = []);
        page.WaitForAssertion(
            () => Assert.Contains("添加 B 站账号后", page.Markup),
            TimeSpan.FromSeconds(5)
        );
        Assert.Empty(page.FindAll("article"));
        Assert.Empty(dashboard.Listeners);
        await page.InvokeAsync(() =>
        {
            dashboard.Accounts = [new(0, "账号1", "synthetic-account-c")];
            dashboard.Read = (_, _, _) => Task.FromResult(Snapshot(account: 2));
        });
        page.WaitForAssertion(
            () => Assert.Contains("账号3主播", page.Markup),
            TimeSpan.FromSeconds(5)
        );
        Assert.Single(dashboard.Listeners);
    }

    [Fact]
    public async Task ReorderingAccountsKeepsSelectedIdentityAndMovesSubscription()
    {
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(50) };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("账号1主播", page.Markup));
        await page.InvokeAsync(() =>
        {
            dashboard.Accounts =
            [
                new(0, "账号1", "synthetic-account-b"),
                new(1, "账号2", "synthetic-account-a"),
            ];
            dashboard.Read = (_, _, _) =>
                Task.FromException<LiveMedalSnapshot>(new IOException("synthetic refresh failure"));
        });
        page.WaitForAssertion(
            () =>
                Assert.Equal("1", page.Find("select[aria-label='查看账号']").GetAttribute("value")),
            TimeSpan.FromSeconds(5)
        );
        Assert.Contains("账号1主播", page.Markup);
        Assert.False(dashboard.Listeners.ContainsKey(0));
        await page.InvokeAsync(() => dashboard.Listeners[1](Snapshot(current: 7, revision: 3)));
        page.WaitForAssertion(() => Assert.Contains("每日上限 7/10", page.Markup));
    }

    [Fact]
    public async Task DuplicateAccountEntriesDoNotUndoManualAccountSelection()
    {
        var dashboard = new Dashboard
        {
            Accounts =
            [
                new(0, "账号1", "synthetic-account-a"),
                new(1, "账号2", "synthetic-account-a"),
            ],
            AutoRefreshInterval = TimeSpan.FromMilliseconds(50),
        };
        var polledSecond = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var secondReads = 0;
        dashboard.Read = (index, _, _) =>
        {
            if (index == 1 && Interlocked.Increment(ref secondReads) >= 2)
                polledSecond.TrySetResult();
            return Task.FromResult(Snapshot(index));
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("账号1主播", page.Markup));
        await page.Find("select[aria-label='查看账号']")
            .ChangeAsync(new ChangeEventArgs { Value = "1" });
        await polledSecond.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("1", page.Find("select[aria-label='查看账号']").GetAttribute("value"));
        Assert.Contains("账号2主播", page.Markup);
    }

    [Fact]
    public async Task AccountReplacementCancelsPendingRefreshAndRejectsItsLateResult()
    {
        var pending = new TaskCompletionSource<LiveMedalSnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(50) };
        dashboard.Read = (_, count, _) =>
        {
            if (count == 1)
                return Task.FromResult(Snapshot());
            started.TrySetResult();
            return pending.Task;
        };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await page.InvokeAsync(() =>
        {
            dashboard.Accounts = [new(0, "账号1", "synthetic-account-c")];
            dashboard.Cached = (_, _) =>
                Task.FromResult<LiveMedalSnapshot?>(Snapshot(account: 2, current: 7, revision: 2));
            dashboard.Read = (_, _, _) =>
                Task.FromException<LiveMedalSnapshot>(new IOException("synthetic refresh failure"));
        });
        page.WaitForAssertion(
            () => Assert.Contains("账号3主播", page.Markup),
            TimeSpan.FromSeconds(5)
        );
        await page.InvokeAsync(async () =>
        {
            pending.SetResult(Snapshot(current: 10, revision: 99));
            await Task.Yield();
        });
        Assert.Contains("账号3主播", page.Markup);
        Assert.DoesNotContain("账号1主播", page.Markup);
        Assert.DoesNotContain("每日上限 10/10", page.Markup);
    }

    [Fact]
    public async Task TransientAccountLookupFailureDoesNotStopAutomaticRefresh()
    {
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(50) };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var page = RenderComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.Contains("每日上限 0/10", page.Markup));
        await page.InvokeAsync(() =>
        {
            dashboard.FailNextAccountRead = 1;
            dashboard.Read = (_, _, _) => Task.FromResult(Snapshot(current: 7, revision: 2));
        });
        page.WaitForAssertion(
            () => Assert.Contains("每日上限 7/10", page.Markup),
            TimeSpan.FromSeconds(5)
        );
        Assert.DoesNotContain("synthetic account lookup failure", page.Markup);
    }
}
