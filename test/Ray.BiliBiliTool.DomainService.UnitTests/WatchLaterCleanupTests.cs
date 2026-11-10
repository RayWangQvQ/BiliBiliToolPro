using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService;
using Refit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class WatchLaterCleanupTests
{
    private static BiliCookie Cookie() =>
        new(
            new Dictionary<string, string>
            {
                ["SESSDATA"] = "sess",
                ["bili_jct"] = "jct",
                ["DedeUserID"] = "1",
            }
        );

    private static WatchLaterItem Item(
        long aid,
        int duration = 100,
        int progress = 0,
        int state = 0,
        string? bvid = null
    ) =>
        new()
        {
            Aid = aid,
            Bvid = bvid ?? $"BV{aid}",
            Title = $"video {aid}",
            Duration = duration,
            Progress = progress,
            State = state,
        };

    /// <summary>
    /// 稍后再看列表接口不返回 has_more，分页必须靠 count 判断，
    /// 否则只会处理第一页（历史 bug：942 条只清前 50 条）。
    /// </summary>
    [Fact]
    public async Task GetAllItemsAsync_PaginatesUntilCountReached()
    {
        var api = new FakeWatchLaterApi
        {
            Pages = { [1] = (4, [Item(1), Item(2)]), [2] = (4, [Item(3), Item(4)]) },
        };

        var service = new WatchLaterDomainService(
            NullLogger<WatchLaterDomainService>.Instance,
            api
        );

        var items = await service.GetAllItemsAsync(Cookie(), pageSize: 2);

        Assert.Equal(4, items.Count);
        Assert.Equal([1, 2], api.RequestedPages);
    }

    [Fact]
    public async Task GetAllItemsAsync_StopsAtLastPage()
    {
        var api = new FakeWatchLaterApi { Pages = { [1] = (2, [Item(1), Item(2)]) } };

        var service = new WatchLaterDomainService(
            NullLogger<WatchLaterDomainService>.Instance,
            api
        );

        var items = await service.GetAllItemsAsync(Cookie(), pageSize: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal([1], api.RequestedPages);
    }

    [Fact]
    public async Task CleanViewedAsync_DeletesViewedAndInvalidOnly()
    {
        var api = new FakeWatchLaterApi
        {
            Pages =
            {
                [1] = (3, [Item(1, progress: 100), Item(2, progress: 10), Item(3, state: 1)]),
            },
        };

        var service = new WatchLaterDomainService(
            NullLogger<WatchLaterDomainService>.Instance,
            api
        );

        var deleted = await service.CleanViewedAsync(
            Cookie(),
            intervalMs: 0,
            useHistoryCrossCheck: false,
            backupBeforeClean: false
        );

        Assert.Equal(2, deleted);
        Assert.Equal([1L, 3L], api.DeletedAids);
    }

    [Fact]
    public async Task CleanViewedAsync_KeepsInvalidItemsWhenDisabled()
    {
        var api = new FakeWatchLaterApi
        {
            Pages =
            {
                [1] = (3, [Item(1, progress: 100), Item(2, progress: 10), Item(3, state: 1)]),
            },
        };

        var service = new WatchLaterDomainService(
            NullLogger<WatchLaterDomainService>.Instance,
            api
        );

        var deleted = await service.CleanViewedAsync(
            Cookie(),
            intervalMs: 0,
            alsoDeleteInvalid: false,
            useHistoryCrossCheck: false,
            backupBeforeClean: false
        );

        Assert.Equal(1, deleted);
        Assert.Equal([1L], api.DeletedAids);
    }

    [Fact]
    public async Task CleanViewedAsync_UsesHistoryWhenToviewProgressIsUnreliable()
    {
        //B 站会把握手进度清成 0/-1，只能靠历史记录判断是否看完
        var api = new FakeWatchLaterApi
        {
            Pages = { [1] = (2, [Item(1, progress: 0), Item(2, progress: 0)]) },
            History =
            {
                [1] = new HistoryItem
                {
                    Progress = 100,
                    Duration = 100,
                    History = new HistoryDetail { Bvid = "BV1" },
                },
            },
        };

        var service = new WatchLaterDomainService(
            NullLogger<WatchLaterDomainService>.Instance,
            api
        );

        var deleted = await service.CleanViewedAsync(
            Cookie(),
            intervalMs: 0,
            useHistoryCrossCheck: true,
            backupBeforeClean: false
        );

        Assert.Equal(1, deleted);
        Assert.Equal([1L], api.DeletedAids);
    }

    /// <summary>
    /// 锁住真实报文解析：接口返回 count / add_at，没有 has_more；
    /// snake_case 字段必须显式映射，否则时间字段会静默丢成 0。
    /// </summary>
    [Fact]
    public async Task WatchLaterPayload_MapsCountAndAddAt()
    {
        const string json = """
            {"code":0,"message":"OK","ttl":1,"data":{"count":942,"list":[{"aid":117534716073861,"bvid":"BV1xx411c7mD","title":"t","duration":991,"progress":0,"state":0,"add_at":1712345678,"owner":{"mid":7,"name":"up"}}]}}
            """;

        var serializer = new SystemTextJsonContentSerializer();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await serializer.FromHttpContentAsync<
            BiliApiResponse<WatchLaterListResponse>
        >(content, CancellationToken.None);

        var data = Assert.IsType<WatchLaterListResponse>(response!.Data);
        Assert.Equal(942, data.Count);
        var item = Assert.Single(data.List!);
        Assert.Equal(117534716073861L, item.Aid);
        Assert.Equal(1712345678L, item.AddTime);
        Assert.Equal(991, item.Duration);
    }

    private sealed class FakeWatchLaterApi : IWatchLaterApi
    {
        public Dictionary<int, (int Count, List<WatchLaterItem> Items)> Pages { get; } = [];

        public Dictionary<long, HistoryItem> History { get; } = [];

        public List<int> RequestedPages { get; } = [];

        public List<long> DeletedAids { get; } = [];

        public Task<BiliApiResponse<WatchLaterListResponse>> GetListAsync(int ps, int pn, string ck)
        {
            RequestedPages.Add(pn);
            var (count, items) = Pages.TryGetValue(pn, out var page) ? page : (0, []);

            return Task.FromResult(
                new BiliApiResponse<WatchLaterListResponse>
                {
                    Code = 0,
                    Data = new WatchLaterListResponse { Count = count, List = items },
                }
            );
        }

        public Task<BiliApiResponse> DeleteAsync(long aid, string viewed, string csrf, string ck)
        {
            DeletedAids.Add(aid);
            return Task.FromResult(new BiliApiResponse { Code = 0 });
        }

        public Task<BiliApiResponse> ClearViewedAsync(string csrf, string ck) =>
            Task.FromResult(new BiliApiResponse { Code = 0 });

        public Task<BiliApiResponse> DeleteByBvidAsync(string bvid, string csrf, string ck) =>
            Task.FromResult(new BiliApiResponse { Code = 0 });

        public Task<BiliApiResponse<HistoryCursorResponse>> GetHistoryCursorAsync(
            int ps,
            long viewAt,
            string ck
        ) =>
            Task.FromResult(
                new BiliApiResponse<HistoryCursorResponse>
                {
                    Code = 0,
                    Data = new HistoryCursorResponse { List = [.. History.Values] },
                }
            );
    }
}
