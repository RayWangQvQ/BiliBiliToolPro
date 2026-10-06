using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class LiveMedalProgressUpdatesTests
{
    private static BiliCookie Cookie(string login = "synthetic") =>
        new(
            new()
            {
                ["DedeUserID"] = "1",
                ["SESSDATA"] = login,
                ["bili_jct"] = "synthetic-csrf",
            }
        );

    private static LiveMedalSnapshot Snapshot(DateTimeOffset at) =>
        new(
            Enumerable
                .Range(1, 2)
                .Select(id => new LiveMedalCard(
                    id,
                    "示例主播",
                    "示例牌",
                    1,
                    true,
                    true,
                    false,
                    [new("like", "点赞30次", "每日上限 0/1", false, 0)],
                    null,
                    RoomId: id,
                    ProgressUpdatedAt: at
                ))
                .ToArray(),
            at
        );

    private static ActivatedMedalResponse Progress(int current) =>
        new()
        {
            Is_lighted = true,
            Task_info =
            [
                new()
                {
                    Jump_type = "like",
                    Title = "点赞30次",
                    Sub_title = $"每日上限 {current}/1",
                    Is_done = current == 1,
                },
            ],
        };

    private static LiveMedalProgressUpdates Hub() =>
        new(NullLogger<LiveMedalProgressUpdates>.Instance);

    [Fact]
    public async Task RunnerPublishesActualRoundProgressWithoutAdditionalApiRequests()
    {
        var cookie = Cookie();
        var hub = Hub();
        var likes = 0;
        var reads = 0;
        hub.Publish(cookie, Snapshot(DateTimeOffset.UtcNow.AddSeconds(-1)));
        var observed = new List<LiveMedalSnapshot>();
        using var subscription = hub.Subscribe(
            LiveMedalProgressUpdates.AccountKey(cookie),
            observed.Add
        );
        using var failing = hub.Subscribe(
            LiveMedalProgressUpdates.AccountKey(cookie),
            _ => throw new IOException("synthetic")
        );
        var api = LiveTaskTestSupport.Proxy.Create<ILiveApi>(
            (method, args) =>
            {
                if (method == "GetActivatedMedalInfo")
                {
                    reads++;
                    return Task.FromResult(
                        new BiliApiResponse<ActivatedMedalResponse>
                        {
                            Code = 0,
                            Data = Progress(likes / 30),
                        }
                    );
                }
                if (method == "GetLiveRoomInfo")
                    return Task.FromResult(
                        new BiliApiResponse<GetLiveRoomInfoResponse>
                        {
                            Code = 0,
                            Data = new() { Live_Status = 1 },
                        }
                    );
                Assert.Equal("LikeLiveRoom", method);
                likes += 10;
                return Task.FromResult(new BiliApiResponse { Code = 0 });
            }
        );
        var runner = new LiveFansMedalTaskRunner(
            api,
            null!,
            NullLogger.Instance,
            new() { UseLiveStateMonitoring = true, DailyLikeNumber = 300 },
            "synthetic",
            (_, _) => Task.CompletedTask,
            progressObserver: hub
        );
        await runner.RunForAnchorAsync(cookie, 1, 1, "like");
        Assert.Equal(30, likes);
        Assert.Equal(2, reads);
        Assert.Equal(2, observed.Count);
        Assert.False(observed[0].Medals[0].Tasks[0].Done);
        Assert.True(observed[1].Medals[0].Tasks[0].Done);
        Assert.Equal(100, observed[1].Medals[0].Tasks[0].Percent);
        Assert.False(observed[1].Medals[1].Tasks[0].Done);
    }

    [Fact]
    public void ProgressIsolatesCredentialsKeepsDeviceEnrichmentAndUnsubscribes()
    {
        var hub = Hub();
        var first = Cookie();
        var other = Cookie("other-synthetic");
        var calls = 0;
        var at = DateTimeOffset.UtcNow.AddSeconds(-2);
        hub.Publish(first, Snapshot(at));
        hub.Publish(other, Snapshot(at));
        var subscription = hub.Subscribe(LiveMedalProgressUpdates.AccountKey(first), _ => calls++);
        first.MergeCurrentCookie("LIVE_BUVID=synthetic-device");
        hub.Report(first, 1, Progress(1), DateTimeOffset.UtcNow);
        Assert.Equal(1, calls);
        Assert.True(hub.Get(first)!.Medals[0].Tasks[0].Done);
        Assert.False(hub.Get(other)!.Medals[0].Tasks[0].Done);
        Assert.Null(hub.Get(Cookie("new-login")));
        subscription.Dispose();
        hub.Report(first, 1, Progress(1), DateTimeOffset.UtcNow);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void OlderReadsCannotOverwriteLiveProgressOrRefreshUnchangedCards()
    {
        var hub = Hub();
        var cookie = Cookie();
        var old = DateTimeOffset.UtcNow.AddMinutes(-6);
        var initial = hub.Publish(cookie, Snapshot(old));
        var observed = DateTimeOffset.UtcNow.AddSeconds(-1);
        hub.Report(cookie, 1, Progress(1), observed);
        var latest = hub.Publish(cookie, Snapshot(old.AddSeconds(1)));
        Assert.True(latest.Medals[0].Tasks[0].Done);
        Assert.Equal(observed, latest.Medals[0].ProgressUpdatedAt);
        Assert.Equal(old.AddSeconds(1), latest.Medals[1].ProgressUpdatedAt);
        Assert.True(latest.Revision > initial.Revision);
        var status = LiveMedalActionStatusEvaluator.Evaluate(
            latest.Medals[1].Tasks[0],
            latest.Medals[1],
            new(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        Assert.Equal(LiveMedalActionState.Unknown, status.State);
        Assert.DoesNotContain("Revision", JsonSerializer.Serialize(latest));
        Assert.Equal(
            0,
            JsonSerializer
                .Deserialize<LiveMedalSnapshot>(JsonSerializer.Serialize(latest))!
                .Revision
        );
    }

    [Fact]
    public void PreviousDayOrFutureReportsDoNotReplaceCurrentProgress()
    {
        var hub = Hub();
        var cookie = Cookie();
        hub.Publish(cookie, Snapshot(DateTimeOffset.UtcNow));
        hub.Report(cookie, 1, Progress(1), DateTimeOffset.UtcNow.AddDays(-1));
        hub.Report(cookie, 1, Progress(1), DateTimeOffset.UtcNow.AddMinutes(2));
        Assert.False(hub.Get(cookie)!.Medals[0].Tasks[0].Done);
    }

    [Fact]
    public async Task ReaderUsesLiveProgressAndRejectsSubscriptionsAfterRelogin()
    {
        var cookie = Cookie();
        var hub = Hub();
        var calls = 0;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["BiliBiliCookies:0"] = cookie.ToString() }
            )
            .Build();
        var cookies = new CookieStrFactory<BiliCookie>(config);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var api = LiveTaskTestSupport.Proxy.Create<ILiveApi>(
            (_, _) => throw new InvalidOperationException("No API calls expected")
        );
        var reader = new LiveMedalDashboardService(
            cookies,
            api,
            cache,
            NullLogger<LiveMedalDashboardService>.Instance,
            updates: hub
        );
        hub.Publish(cookie, Snapshot(DateTimeOffset.UtcNow.AddSeconds(-1)));
        using var subscription = reader.Subscribe(0, _ => calls++);
        hub.Report(cookie, 1, Progress(1), DateTimeOffset.UtcNow);
        Assert.True((await reader.GetCachedAsync(0))!.Medals[0].Tasks[0].Done);
        Assert.Equal(1, calls);
        config["BiliBiliCookies:0"] = Cookie("new-login").ToString();
        hub.Report(cookie, 1, Progress(1), DateTimeOffset.UtcNow);
        Assert.Equal(1, calls);
        Assert.Null(await reader.GetCachedAsync(0));
    }
}
