using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class UnfollowOutcomeTests
{
    [Theory]
    [InlineData("tags-rejected", "获取关注分组")]
    [InlineData("tags-rejected-with-data", "获取关注分组")]
    [InlineData("tags-missing-data", "获取关注分组")]
    [InlineData("page-rejected", "获取分组关注")]
    [InlineData("page-rejected-with-data", "获取分组关注")]
    [InlineData("later-page-rejected", "获取分组关注")]
    [InlineData("later-page-rejected-with-data", "获取分组关注")]
    [InlineData("tags-null-entry", "获取关注分组")]
    [InlineData("tags-unconfirmed", "获取关注分组")]
    [InlineData("page-missing-data", "获取分组关注")]
    [InlineData("page-null-entry", "获取分组关注")]
    [InlineData("page-zero-id", "获取分组关注")]
    [InlineData("page-negative-id", "获取分组关注")]
    [InlineData("later-page-missing-data", "获取分组关注")]
    public async Task FailedRead_StopsBeforeUnfollowing(string scenario, string expected)
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() => fixture.Run());
        Assert.Contains(expected, error.Message);
        Assert.Empty(fixture.Modifications);
    }

    [Theory]
    [InlineData("modify-rejected")]
    [InlineData("modify-unconfirmed")]
    public async Task RejectedUnfollow_ReportsFailureWithoutRetry(string scenario)
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() => fixture.Run());
        Assert.Contains("失败", error.Message);
        Assert.Single(fixture.Modifications);
        Assert.Equal(0, fixture.Confirmed);
    }

    [Fact]
    public async Task ValidGroup_UnfollowsOnlyConfiguredCountInOriginalOrder()
    {
        var fixture = new Fixture("valid");
        await fixture.Run();
        Assert.Equal(43L, Assert.Single(fixture.Modifications).Fid);
        Assert.Equal(1, fixture.Confirmed);
    }

    [Theory]
    [InlineData("page-network")]
    [InlineData("modify-network")]
    public async Task NetworkFailure_PropagatesWithoutRetry(string scenario)
    {
        var fixture = new Fixture(scenario, count: 2);
        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Run());
        Assert.Equal(scenario == "page-network" ? 0 : 1, fixture.Modifications.Count);
        Assert.Equal(0, fixture.Confirmed);
    }

    [Fact]
    public async Task MixedResults_ReportConfirmedSuccessAndFailures()
    {
        var fixture = new Fixture("modify-mixed", count: 2);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() => fixture.Run());
        Assert.Contains("成功 1 人", error.Message);
        Assert.Contains("失败 1 人", error.Message);
        Assert.Contains("-101", error.Message);
        Assert.Equal(2, fixture.Modifications.Count);
        Assert.Equal(1, fixture.Confirmed);
    }

    [Theory]
    [InlineData("after-query-rejected")]
    [InlineData("after-query-rejected-with-data")]
    [InlineData("after-query-missing-data")]
    public async Task FinalQueryFailure_PreservesConfirmedCountAndDoesNotRepeatWrites(
        string scenario
    )
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() => fixture.Run());
        Assert.Contains("已成功取关 1 人", error.Message);
        Assert.Contains("剩余数量未确认", error.Message);
        Assert.Single(fixture.Modifications);
        Assert.Equal(1, fixture.Confirmed);
    }

    [Theory]
    [InlineData("zero-count")]
    [InlineData("missing-group")]
    [InlineData("empty-group")]
    public async Task NoTargets_PerformsNoUnfollowOrPageQueries(string scenario)
    {
        var fixture = new Fixture(scenario, count: scenario == "zero-count" ? 0 : 1);
        await fixture.Run();
        Assert.Empty(fixture.Modifications);
        Assert.Equal(0, fixture.Pages);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public async Task AllOrLargeCount_StopsAtAvailableAccounts(int count)
    {
        var fixture = new Fixture("valid", count: count);
        await fixture.Run();
        Assert.Equal(2, fixture.Modifications.Count);
        Assert.Equal(2, fixture.Confirmed);
        Assert.Equal(43L, fixture.Modifications[0].Fid);
        Assert.Equal(42L, fixture.Modifications[1].Fid);
    }

    [Fact]
    public async Task RetainList_PreservesProtectedAccount()
    {
        var fixture = new Fixture("valid", count: 2, retain: "43");
        await fixture.Run();
        Assert.Equal(42L, Assert.Single(fixture.Modifications).Fid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task BuiltInGroups_RemainSupported(long tagId)
    {
        var fixture = new Fixture("valid", tagId: tagId);
        await fixture.Run();
        Assert.Equal(tagId, Assert.Single(fixture.PageGroups));
        Assert.Single(fixture.Modifications);
    }

    [Theory]
    [InlineData("duplicate-pages")]
    [InlineData("duplicate-single-page")]
    public async Task DuplicateAccounts_AreUnfollowedOnce(string scenario)
    {
        var fixture = new Fixture(scenario, count: 3);
        await fixture.Run();
        Assert.Equal(
            fixture.Modifications.Count,
            fixture.Modifications.Select(x => x.Fid).Distinct().Count()
        );
        Assert.Equal(2, fixture.Modifications.Count);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(int.MinValue)]
    public async Task InvalidConfiguredCount_StopsBeforeAnyQuery(int count)
    {
        var fixture = new Fixture("valid", count: count);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() => fixture.Run());
        Assert.Contains("执行数量", error.Message);
        Assert.Equal(0, fixture.Tags);
        Assert.Empty(fixture.Modifications);
    }

    [Fact]
    public async Task InvalidServerCount_StopsBeforePageQueries()
    {
        var fixture = new Fixture("negative-total");
        await Assert.ThrowsAsync<BiliBusinessException>(() => fixture.Run());
        Assert.Equal(0, fixture.Pages);
        Assert.Empty(fixture.Modifications);
    }

    private static BiliCookie Cookie =>
        new(new() { ["DedeUserID"] = "123456", ["bili_jct"] = "synthetic-csrf" });

    private sealed class Fixture
    {
        private readonly AccountDomainService _service;
        public List<ModifyRelationRequest> Modifications { get; } = [];
        public int Confirmed { get; private set; }
        public int Tags { get; private set; }
        public int Pages { get; private set; }
        public List<long> PageGroups { get; } = [];
        public CaptureLogger<AccountDomainService> Logger { get; } = new();

        public Fixture(string scenario, int? count = null, long tagId = 51, string? retain = null)
        {
            bool paginated = scenario.StartsWith("later-page-") || scenario == "duplicate-pages";
            var api = ApiStub.Create<IApiApi>(
                (name, args) =>
                {
                    if (name == nameof(IApiApi.GetTags))
                    {
                        Tags++;
                        bool finalError = scenario.StartsWith("after-query-") && Tags == 2;
                        return Task.FromResult(
                            new BiliApiResponse<List<TagDto>>
                            {
                                Code =
                                    scenario.StartsWith("tags-rejected")
                                    || finalError && scenario != "after-query-missing-data"
                                        ? -101
                                    : scenario == "tags-unconfirmed" ? int.MinValue
                                    : 0,
                                Data =
                                    scenario is "tags-rejected" or "tags-missing-data"
                                    || finalError && scenario != "after-query-rejected-with-data"
                                        ? null
                                    : scenario == "tags-null-entry" ? [null!]
                                    : scenario == "missing-group" ? []
                                    :
                                    [
                                        new()
                                        {
                                            Name = "天选时刻",
                                            Tagid = tagId,
                                            Count =
                                                scenario == "empty-group" ? 0
                                                : scenario == "negative-total" ? -1
                                                : (paginated ? 21 : 2) - Confirmed,
                                        },
                                    ],
                            }
                        );
                    }
                    if (name == nameof(IApiApi.GetFollowingsByTag))
                    {
                        Pages++;
                        PageGroups.Add(((GetSpecialFollowingsRequest)args![0]!).Tagid);
                        if (scenario == "page-network")
                            return Task.FromException<BiliApiResponse<List<UpInfo>>>(
                                new HttpRequestException("synthetic-network")
                            );
                        bool rejected =
                            scenario.StartsWith("page-rejected")
                            || scenario.StartsWith("later-page-rejected") && Pages == 2;
                        return Task.FromResult(
                            new BiliApiResponse<List<UpInfo>>
                            {
                                Code = rejected ? -101 : 0,
                                Data =
                                    rejected && !scenario.EndsWith("with-data")
                                    || scenario == "page-missing-data"
                                    || scenario == "later-page-missing-data" && Pages == 2
                                        ? null
                                    : scenario == "page-null-entry" ? [null!]
                                    : scenario is "page-zero-id" or "page-negative-id"
                                        ?
                                        [
                                            new()
                                            {
                                                Mid = scenario == "page-zero-id" ? 0 : -1,
                                                Uname = "synthetic-invalid",
                                            },
                                        ]
                                    : paginated && !(scenario == "duplicate-pages" && Pages == 2)
                                        ?
                                        [
                                            new()
                                            {
                                                Mid = Pages == 1 ? 42 : 43,
                                                Uname = "synthetic-target",
                                            },
                                        ]
                                    : scenario == "duplicate-single-page"
                                        ?
                                        [
                                            new() { Mid = 42, Uname = "synthetic-first" },
                                            new() { Mid = 43, Uname = "synthetic-second" },
                                            new() { Mid = 42, Uname = "synthetic-duplicate" },
                                        ]
                                    :
                                    [
                                        new() { Mid = 42, Uname = "synthetic-first" },
                                        new() { Mid = 43, Uname = "synthetic-second" },
                                    ],
                            }
                        );
                    }
                    if (name == nameof(IApiApi.ModifyRelation))
                    {
                        Modifications.Add((ModifyRelationRequest)args![0]!);
                        if (scenario == "modify-network")
                            return Task.FromException<BiliApiResponse>(
                                new HttpRequestException("synthetic-network")
                            );
                        int code =
                            scenario.StartsWith("modify-rejected")
                            || scenario == "modify-mixed" && Modifications[^1].Fid == 43
                                ? -101
                            : scenario == "modify-unconfirmed" ? int.MinValue
                            : 0;
                        if (code == 0)
                            Confirmed++;
                        return Task.FromResult(
                            new BiliApiResponse
                            {
                                Code = code,
                                Message = code != 0 ? "synthetic-rejection" : null,
                            }
                        );
                    }
                    throw new InvalidOperationException("Unexpected synthetic API call: " + name);
                }
            );
            _service = new AccountDomainService(
                Logger,
                api,
                null!,
                new TestOptions<UnfollowBatchedTaskOptions>(
                    new() { Count = count ?? (paginated ? 3 : 1), RetainUids = retain }
                ),
                new TestOptions<DailyTaskOptions>(new())
            );
        }

        public Task Run() => _service.UnfollowBatched(Cookie);
    }
}
