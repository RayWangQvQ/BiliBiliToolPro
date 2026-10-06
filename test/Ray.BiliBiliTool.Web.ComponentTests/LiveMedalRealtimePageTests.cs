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
        public Dictionary<int, Action<LiveMedalSnapshot>> Listeners = [];

        public IReadOnlyList<LiveMedalAccount> GetAccounts() => [new(0, "账号1"), new(1, "账号2")];

        public Task<LiveMedalSnapshot?> GetCachedAsync(
            int index,
            CancellationToken token = default
        ) => Task.FromResult<LiveMedalSnapshot?>(null);

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
                .Single(button => button.TextContent.Contains("保存配置"))
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
        var dashboard = new Dashboard { AutoRefreshInterval = TimeSpan.FromMilliseconds(50) };
        Services.AddSingleton<ILiveMedalDashboardService>(dashboard);
        var host = RenderComponent<PageHost>();
        var page = host.FindComponent<LiveFansMedalTaskConfig>();
        page.WaitForAssertion(() => Assert.True(dashboard.Reads >= 2));
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
}
