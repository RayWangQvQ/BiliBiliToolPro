using System.Reflection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveLotteryGroupingTests
{
    [Theory]
    [InlineData("query-rejected", "获取关注分组")]
    [InlineData("query-rejected-with-data", "获取关注分组")]
    [InlineData("query-missing-data", "获取关注分组")]
    [InlineData("create-rejected", "创建")]
    [InlineData("create-missing-data", "创建")]
    [InlineData("create-zero-id", "创建")]
    [InlineData("create-negative-id", "创建")]
    [InlineData("existing-zero-id", "无效")]
    [InlineData("existing-negative-id", "无效")]
    [InlineData("query-null-entry", "获取关注分组")]
    public async Task FailedGroupLookupOrCreation_StopsBeforeMovingUsers(
        string scenario,
        string expected
    )
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() =>
            fixture.Service.GroupFollowing(Cookie)
        );
        Assert.Contains(expected, error.Message);
        Assert.Empty(fixture.CopyRequests);
        if (scenario.StartsWith("query-"))
            Assert.Equal(0, fixture.Creates);
        Assert.DoesNotContain(
            fixture.Logger.Messages,
            text => text.Contains("创建成功") || text.Contains("全部成功")
        );
    }

    [Fact]
    public async Task ExistingGroup_MovesOnlySelectedNewFollowings()
    {
        var fixture = new Fixture("existing-valid");
        await fixture.Service.GroupFollowing(Cookie);
        var request = Assert.Single(fixture.CopyRequests);
        Assert.Equal("51", request.Tagids);
        Assert.Equal("42", request.Fids);
        Assert.Equal(0, fixture.Creates);
        Assert.Contains(fixture.Logger.Messages, text => text.Contains("全部成功"));
    }

    [Fact]
    public async Task MissingGroup_CreatesOnceAndMovesToConfirmedId()
    {
        var fixture = new Fixture("create-valid");
        await fixture.Service.GroupFollowing(Cookie);
        Assert.Equal(1, fixture.Creates);
        Assert.Equal("https://space.bilibili.com/123456/fans/follow", fixture.CreateReferer);
        Assert.Equal("52", Assert.Single(fixture.CopyRequests).Tagids);
        Assert.Contains(fixture.Logger.Messages, text => text.Contains("创建成功"));
    }

    [Theory]
    [InlineData("following-rejected")]
    [InlineData("following-rejected-with-data")]
    [InlineData("following-missing-data")]
    [InlineData("following-null-list")]
    [InlineData("following-null-entry")]
    public async Task FailedFollowingQuery_StopsBeforeGroupLookupAndWrites(string scenario)
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() =>
            fixture.Service.GroupFollowing(Cookie)
        );
        Assert.Contains("获取关注列表", error.Message);
        Assert.Equal(0, fixture.TagQueries);
        Assert.Equal(0, fixture.Creates);
        Assert.Empty(fixture.CopyRequests);
    }

    [Theory]
    [InlineData("following-rejected")]
    [InlineData("following-null-list")]
    public async Task InitialFollowingQueryFailure_StopsBeforeLotteryActivities(string scenario)
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() =>
            fixture.Service.TianXuan(Cookie)
        );
        Assert.Contains("获取关注列表", error.Message);
        Assert.Empty(fixture.CopyRequests);
    }

    [Theory]
    [InlineData("copy-rejected", -101)]
    [InlineData("copy-unconfirmed", int.MinValue)]
    public async Task FailedMove_IsReportedAsFailureWithoutRetry(string scenario, int code)
    {
        var fixture = new Fixture(scenario);
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() =>
            fixture.Service.GroupFollowing(Cookie)
        );
        Assert.Contains(code.ToString(), error.Message);
        Assert.Single(fixture.CopyRequests);
        Assert.DoesNotContain(fixture.Logger.Messages, text => text.Contains("全部成功"));
    }

    [Theory]
    [InlineData("empty-followings")]
    [InlineData("no-lottery-followings")]
    public async Task NoNewTargets_DoesNotQueryOrCreateGroups(string scenario)
    {
        var fixture = new Fixture(scenario);
        await fixture.Service.GroupFollowing(Cookie);
        Assert.Equal(0, fixture.TagQueries);
        Assert.Equal(0, fixture.Creates);
        Assert.Empty(fixture.CopyRequests);
    }

    private static BiliCookie Cookie =>
        new(new() { ["DedeUserID"] = "123456", ["bili_jct"] = "synthetic-csrf" });

    private sealed class Fixture
    {
        public CaptureLogger<LiveDomainService> Logger { get; } = new();
        public LiveDomainService Service { get; }
        public List<CopyUserToGroupRequest> CopyRequests { get; } = [];
        public int Creates { get; private set; }
        public int TagQueries { get; private set; }
        public string? CreateReferer { get; private set; }

        public Fixture(string scenario)
        {
            var api = ApiStub.Create<IApiApi>(
                (name, args) =>
                {
                    if (name == nameof(IApiApi.GetFollowings))
                        return Task.FromResult(
                            new BiliApiResponse<GetFollowingsResponse>
                            {
                                Code = scenario.StartsWith("following-rejected") ? -101 : 0,
                                Data = scenario is "following-rejected" or "following-missing-data"
                                    ? null
                                    : new()
                                    {
                                        List =
                                            scenario == "following-null-list" ? null!
                                            : scenario == "following-null-entry" ? [null!]
                                            : scenario == "empty-followings" ? []
                                            :
                                            [
                                                new() { Mid = 42, Uname = "synthetic-target" },
                                                new() { Mid = 43, Uname = "synthetic-other" },
                                                new() { Mid = 99, Uname = "synthetic-boundary" },
                                            ],
                                    },
                            }
                        );
                    if (name == nameof(IApiApi.GetTags))
                    {
                        TagQueries++;
                        return Task.FromResult(
                            new BiliApiResponse<List<TagDto>>
                            {
                                Code = scenario.StartsWith("query-rejected") ? -101 : 0,
                                Data =
                                    scenario is "query-rejected" or "query-missing-data" ? null
                                    : scenario == "query-null-entry" ? [null!]
                                    : scenario.StartsWith("existing")
                                    || scenario.StartsWith("copy-")
                                    || scenario == "query-rejected-with-data"
                                        ?
                                        [
                                            new()
                                            {
                                                Name = "天选时刻",
                                                Tagid =
                                                    scenario == "existing-zero-id" ? 0
                                                    : scenario == "existing-negative-id" ? -1
                                                    : 51,
                                            },
                                        ]
                                    : [],
                            }
                        );
                    }
                    if (name == nameof(IApiApi.CreateTag))
                    {
                        Creates++;
                        CreateReferer = (string?)args![2];
                        return Task.FromResult(
                            new BiliApiResponse<CreateTagResponse>
                            {
                                Code = scenario == "create-rejected" ? 22103 : 0,
                                Message =
                                    scenario == "create-rejected" ? "synthetic-group-limit" : null,
                                Data = scenario is "create-rejected" or "create-missing-data"
                                    ? null
                                    : new()
                                    {
                                        Tagid =
                                            scenario == "create-zero-id" ? 0
                                            : scenario == "create-negative-id" ? -1
                                            : 52,
                                    },
                            }
                        );
                    }
                    if (name == nameof(IApiApi.CopyUpsToGroup))
                    {
                        CopyRequests.Add((CopyUserToGroupRequest)args![0]!);
                        return Task.FromResult(
                            new BiliApiResponse
                            {
                                Code =
                                    scenario == "copy-rejected" ? -101
                                    : scenario == "copy-unconfirmed" ? int.MinValue
                                    : 0,
                            }
                        );
                    }
                    throw new InvalidOperationException("Unexpected synthetic API call: " + name);
                }
            );
            Service = new LiveDomainService(
                Logger,
                null!,
                api,
                null!,
                new TestOptions<DailyTaskOptions>(new()),
                new TestOptions<LiveLotteryTaskOptions>(new()),
                new TestOptions<LiveFansMedalTaskOptions>(new()),
                new TestOptions<SecurityOptions>(new()),
                new TestOptions<Silver2CoinTaskOptions>(new())
            );
            typeof(LiveDomainService)
                .GetField("_tianXuanFollowed", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(
                    Service,
                    scenario == "no-lottery-followings"
                        ? new List<ListItemDto>()
                        : new List<ListItemDto>
                        {
                            new()
                            {
                                Uid = 42,
                                Uname = "synthetic-target",
                                Title = "synthetic-room",
                                Parent_name = "synthetic-area",
                            },
                        }
                );
            typeof(LiveDomainService)
                .GetField("_lastFollowUpId", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Service, 99L);
        }
    }
}
