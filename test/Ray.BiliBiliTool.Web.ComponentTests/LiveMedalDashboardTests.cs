using System.Reflection;
using Bunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Web.Components.Comps;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class LiveMedalDashboardTests : TestContext
{
    public LiveMedalDashboardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Cards_ShowLevelsProgressAndKeepExcludedAnchorsVisible()
    {
        var cut = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "示例账号")])
                .Add(x => x.Snapshot, Example())
                .Add(x => x.ExcludedAnchorIds, "22")
        );
        Assert.Contains("Lv.60", cut.Markup);
        Assert.Contains("每日上限 7/10", cut.Markup);
        Assert.Contains("已完成", cut.Markup);
        Assert.Equal(2, cut.FindAll("article").Count);
        Assert.Contains("已排除", cut.Find("article[data-anchor='22']").TextContent);
        Assert.False(cut.Find("article[data-anchor='11'] input").HasAttribute("checked"));
        Assert.True(cut.Find("article[data-anchor='22'] input").HasAttribute("checked"));
        Assert.False(cut.Find("select[aria-label=查看账号]").HasAttribute("disabled"));
        Assert.Equal("0", cut.Find("select[aria-label=查看账号]").GetAttribute("value"));
        Assert.Equal(
            "70",
            cut.Find("article[data-anchor='11'] [role=progressbar]").GetAttribute("aria-valuenow")
        );
        cut.Find("input[type=search]").Input("星河");
        Assert.Single(cut.FindAll("article"));

        var directory = Environment.GetEnvironmentVariable("MEDAL_PREVIEW_DIR");
        if (directory is not null)
        {
            cut.Find("input[type=search]").Input("");
            var theme = RenderComponent<MudThemeProvider>();
            var guide = RenderComponent<TaskGuide>(p =>
                p.Add(x => x.TaskKey, "LiveFansMedalAppService")
            );
            var brief = RenderComponent<TaskBrief>(p =>
                p.Add(x => x.TaskKey, "DailyTaskAppService").Add(x => x.ItemKey, "DonateCoin")
            );
            var schedule = RenderComponent<ScheduleTimePicker>(p =>
                p.Add(x => x.Value, "0 5 0 * * ?")
            );
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "preview.html"),
                "<!doctype html><html lang='zh-CN'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><link rel='stylesheet' href='MudBlazor.min.css'><link rel='stylesheet' href='app.css'></head><body>"
                    + theme.Markup
                    + "<main style='max-width:1060px;margin:28px auto;padding:0 16px'><h1 style='font-size:24px;margin-bottom:20px'>直播粉丝牌任务配置</h1>"
                    + guide.Markup
                    + cut.Markup
                    + "<section style='padding:24px;border-radius:16px;border:1px solid var(--mud-palette-lines-default);background:var(--mud-palette-surface);max-width:560px'>"
                    + schedule.Markup
                    + "</section><section style='padding:24px;margin-top:24px;border-radius:12px;border:1px solid var(--mud-palette-lines-default)'><h2 style='font-size:18px'>今日任务 · 投币</h2>"
                    + brief.Markup
                    + "</section><p style='font-size:12px;margin-top:16px;color:#777'>界面预览 · 示例数据</p></main></body></html>"
            );
        }
    }

    [Fact]
    public void ExclusionTogglePreservesSelectionsAcrossAccounts()
    {
        string? result = null;
        var cut = RenderComponent<LiveMedalDashboard>(p =>
            p.Add(x => x.Accounts, [new(0, "账号 1")])
                .Add(x => x.Snapshot, Example())
                .Add(x => x.ExcludedAnchorIds, "22,333")
                .Add(x => x.ExcludedAnchorIdsChanged, (string value) => result = value)
        );
        cut.Find("article[data-anchor='11'] input").Change(true);
        Assert.Equal("11,22,333", result);
        cut.SetParametersAndRender(p => p.Add(x => x.ExcludedAnchorIds, result!));
        cut.Find("article[data-anchor='22'] input").Change(false);
        Assert.Equal("11,333", result);
    }

    [Fact]
    public async Task Reader_PaginatesDeduplicatesCachesAndRefreshesWithoutActivities()
    {
        var reads = 0;
        var config = Configuration();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Service(
            config,
            cache,
            (method, args) =>
                method switch
                {
                    "GetFansMedalPanel" => Task.FromResult(
                        new BiliApiResponse<FansMedalPanelResponse>
                        {
                            Code = 0,
                            Data = new()
                            {
                                List = [Panel(11)],
                                Special_list = [Panel(11)],
                                Page_info = new() { Total_page = 2 },
                            },
                        }
                    ),
                    "GetActivatedMedalInfo" => Tasks(++reads, 0),
                    _ => throw new InvalidOperationException("No activity calls are allowed"),
                }
        );
        var first = await service.GetAsync(0);
        Assert.Single(first.Medals);
        Assert.Equal(60, first.Medals[0].Level);
        Assert.Equal("星河", first.Medals[0].AnchorName);
        Assert.Equal(70, first.Medals[0].Tasks[0].Percent);
        Assert.Same(first, await service.GetAsync(0));
        Assert.Equal(1, reads);
        await service.GetAsync(0, refresh: true);
        Assert.Equal(2, reads);
        config["BiliBiliCookies:0"] = "DedeUserID=1;bili_jct=new-synthetic;SESSDATA=new-synthetic";
        await service.GetAsync(0);
        Assert.Equal(3, reads);
        await service.GetAsync(1);
        Assert.Equal(4, reads);
    }

    [Theory]
    [InlineData(-101, "请重新登录")]
    [InlineData(-500, "暂未加载")]
    public async Task Reader_FailedTaskReadsKeepMedalAndDoNotShowFakeCompletion(
        int code,
        string message
    )
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = Service(
            Configuration(),
            cache,
            (method, _) =>
                method == "GetFansMedalPanel"
                    ? Task.FromResult(
                        new BiliApiResponse<FansMedalPanelResponse>
                        {
                            Code = 0,
                            Data = new() { List = [Panel(11)] },
                        }
                    )
                    : Tasks(0, code)
        );
        var snapshot = await service.GetAsync(0);
        var medal = Assert.Single(snapshot.Medals);
        Assert.Contains(message, medal.Error);
        Assert.Empty(medal.Tasks);
        Assert.Null(medal.Lighted);
        Assert.Null(snapshot.Error);
    }

    [Fact]
    public void UnquantifiedLightingProgressDoesNotInventPercentage()
    {
        var task = LiveMedalTaskProgress.From(
            new() { Title = "发送弹幕1次", Sub_title = "完成即可点亮" }
        );
        Assert.Null(task.Percent);
        Assert.False(task.Done);
    }

    private static IConfigurationRoot Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "DedeUserID=1;bili_jct=synthetic;SESSDATA=synthetic",
                    ["BiliBiliCookies:1"] =
                        "DedeUserID=2;bili_jct=synthetic-2;SESSDATA=synthetic-2",
                }
            )
            .Build();

    private static LiveMedalDashboardService Service(
        IConfiguration config,
        IMemoryCache cache,
        Func<string, object?[], object> handler
    ) =>
        new(
            new CookieStrFactory<BiliCookie>(config),
            ApiProxy.Make(handler),
            cache,
            NullLogger<LiveMedalDashboardService>.Instance
        );

    private static FansMedalPanelItem Panel(long id) =>
        new()
        {
            Medal = new()
            {
                Target_id = id,
                Level = 60,
                Medal_name = "星光",
            },
            Anchor_info = new() { Nick_name = "星河" },
        };

    private static Task<BiliApiResponse<ActivatedMedalResponse>> Tasks(int unused, int code) =>
        Task.FromResult(
            new BiliApiResponse<ActivatedMedalResponse>
            {
                Code = code,
                Data = new()
                {
                    Is_lighted = true,
                    Task_info =
                    [
                        new()
                        {
                            Title = "点赞30次",
                            Sub_title = "每日上限 7/10",
                            Jump_type = "like",
                        },
                    ],
                },
            }
        );

    internal static LiveMedalSnapshot Example() =>
        new(
            [
                new(
                    11,
                    "星河",
                    "星光",
                    60,
                    true,
                    true,
                    false,
                    [
                        new("like", "点赞30次", "每日上限 7/10", false, 70),
                        new("sendDanmu", "发送弹幕1次", "每日上限 10/10", true, 100),
                        new("watchLive", "观看直播15分钟", "每日上限 3/10", false, 30),
                    ],
                    null
                ),
                new(
                    22,
                    "山间晚风",
                    "晚风",
                    18,
                    false,
                    true,
                    false,
                    [
                        new("like", "点赞30次", "每日上限 10/10", true, 100),
                        new("sendDanmu", "发送弹幕1次", "每日上限 10/10", true, 100),
                        new("watchLive", "观看直播15分钟", "每日上限 10/10", true, 100),
                    ],
                    null
                ),
            ],
            DateTimeOffset.Parse("2026-10-05T08:30:00+08:00")
        );

    public class ApiProxy : DispatchProxy
    {
        public Func<string, object?[], object> Handler { get; set; } = null!;

        protected override object Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method!.Name, args!);

        public static ILiveApi Make(Func<string, object?[], object> handler)
        {
            var proxy = Create<ILiveApi, ApiProxy>();
            ((ApiProxy)(object)proxy).Handler = handler;
            return proxy;
        }
    }
}
