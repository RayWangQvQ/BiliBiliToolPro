using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Charge;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

[Trait("Category", "External")]
public class ApiApiTests : IDisposable
{
    private readonly IHost _host;

    private readonly IServiceScope _scope;

    private readonly IApiApi _api;

    private readonly BiliCookie _ck;

    private readonly IWbiService _wbiService;

    public ApiApiTests()
    {
        _host = Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        _scope = _host.Services.CreateScope();
        _ck = ExternalCookie.Require(_scope.ServiceProvider);
        _api = _scope.ServiceProvider.GetRequiredService<IApiApi>();
        _wbiService = _scope.ServiceProvider.GetRequiredService<IWbiService>();
    }

    #region UpInfo

    [Fact]
    public async Task GetSpaceInfo_Normal_Success()
    {
        var req = new GetSpaceInfoDto { mid = 919174L };

        BiliApiResponse<GetSpaceInfoResponse> re = await _api.GetSpaceInfo(req, _ck.ToString());

        Assert.True(re.Code == 0);
        Assert.NotNull(re.Data);
        Assert.Equal(919174, re.Data.Mid);
        Assert.NotNull(re.Data.Live_room);
        Assert.Equal(3115258, re.Data.Live_room.Roomid);
        Assert.False(string.IsNullOrEmpty(re.Data.Name));
        Assert.False(string.IsNullOrEmpty(re.Data.Live_room.Title));
    }

    #endregion

    #region 每日任务

    [Fact]
    public async Task GetDailyTaskRewardInfo_Normal_Success()
    {
        BiliApiResponse<DailyTaskInfo> re = await _api.GetDailyTaskRewardInfoAsync(_ck.ToString());

        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDonateCoinExp_Normal_Success()
    {
        BiliApiResponse<int> re = await _api.GetDonateCoinExpAsync(_ck.ToString());

        re.Code.Should().Be(0);
        re.Data.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ReceiveVipPrivilege_Normal_Success()
    {
        BiliApiResponse re = await _api.ReceiveVipPrivilegeAsync(
            (int)VipPrivilegeType.BCoinCoupon,
            _ck.BiliJct,
            _ck.ToString()
        );

        re.Code.Should()
            .BeOneOf(
                0,
                73319, //todo: sort out meanings
                69801 //你已领取过该权益
            );
    }

    #endregion

    #region 关注

    [Fact]
    public async Task GetFollowings_Normal_Success()
    {
        var req = new GetFollowingsRequest(long.Parse(_ck.UserId));

        BiliApiResponse<GetFollowingsResponse> re = await _api.GetFollowings(req, _ck.ToString());

        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data!.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetFollowingsByTag_Normal_Success()
    {
        var req = new GetSpecialFollowingsRequest(long.Parse(_ck.UserId));

        BiliApiResponse<List<UpInfo>> re = await _api.GetFollowingsByTag(req, _ck.ToString());

        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTags_Normal_Success()
    {
        BiliApiResponse<List<TagDto>> re = await _api.GetTags(_ck.ToString());

        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTag_Normal_Success()
    {
        var req = new CreateTagRequest { Tag = "auto_test", Csrf = _ck.BiliJct };

        BiliApiResponse<CreateTagResponse> re = await _api.CreateTag(req, _ck.ToString());

        re.Code.Should().Be(0);
        re.Data!.Tagid.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CopyUpsToGroup_Normal_Success()
    {
        var req = new CopyUserToGroupRequest(new List<long> { 919174L }, "-1", _ck.BiliJct);

        BiliApiResponse re = await _api.CopyUpsToGroup(req, _ck.ToString());

        re.Code.Should().Be(0);
    }

    [Fact]
    public async Task ModifyRelation_Normal_Success()
    {
        var req = new ModifyRelationRequest(919174L, _ck.BiliJct);

        BiliApiResponse re = await _api.ModifyRelation(req, _ck.ToString());

        re.Code.Should().BeOneOf(0, -101);
    }

    #endregion

    #region 充电

    [Fact]
    public async Task ChargeV2Async_SendRequest_NotEnough()
    {
        var upId = 220893216;
        var req = new ChargeRequest(2, upId, _ck.BiliJct);

        BiliApiResponse<ChargeV2Response> re = await _api.ChargeV2Async(req, _ck.ToString());

        re.Code.Should().Be(0);
        re.Data!.Status.Should()
            .BeOneOf(
                -4, //bp.to.battery http failed, invalid args, errNo=800409904
                4
            );
    }

    [Fact]
    public async Task ChargeCommentAsync_Normal_Success()
    {
        var req = new ChargeCommentRequest("auto_test_order", "auto_test_comment", _ck.BiliJct);

        BiliApiResponse<ChargeResponse> re = await _api.ChargeCommentAsync(req, _ck.ToString());

        re.Code.Should().BeOneOf(0, -101);
    }

    #endregion

    #region 视频

    [Fact]
    public async Task ShareVideo_Normal_Success()
    {
        var req = new ShareVideoRequest(248097491, _ck.BiliJct);

        BiliApiResponse re = await _api.ShareVideo(req, _ck.ToString());

        re.Code.Should().BeOneOf(0, -101, 71000);
    }

    [Fact]
    public async Task UploadVideoHeartbeat_Normal_Success()
    {
        var req = new UploadVideoHeartbeatRequest
        {
            Aid = 248097491,
            Bvid = "BV1Bv411s7xN",
            Mid = long.Parse(_ck.UserId),
            Csrf = _ck.BiliJct,
            Played_time = 10,
        };

        BiliApiResponse re = await _api.UploadVideoHeartbeat(
            req.Aid,
            req.Played_time,
            req,
            _ck.ToString()
        );

        re.Code.Should().Be(0);
    }

    [Fact]
    public async Task AddCoinForVideo_Normal_Success()
    {
        var req = new AddCoinRequest(248097491, _ck.BiliJct);

        BiliApiResponse re = await _api.AddCoinForVideo(req, _ck.ToString());

        re.Code.Should().BeOneOf(0, 34005);
    }

    [Fact]
    public async Task GetDonatedCoinsForVideo_ValidVideo_ReportsCoinCount()
    {
        var req = new GetAlreadyDonatedCoinsRequest(248097491);

        BiliApiResponse<DonatedCoinsForVideo>? re = await _api.GetDonatedCoinsForVideo(
            req,
            _ck.ToString()
        );

        Assert.Equal(0, re.Code);
        Assert.NotNull(re.Data);
        Assert.True(re.Data.Multiply >= 0);
    }

    [Fact]
    public async Task SearchVideosByUpId_Normal_Success()
    {
        var req = new SearchVideosByUpIdDto { mid = 1585227649 };
        await _wbiService.SetWridAsync(req, _ck);

        BiliApiResponse<SearchUpVideosResponse> re = await _api.SearchVideosByUpId(
            req,
            _ck.ToString()
        );

        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBangumiBySsid_ValidSsid_ReturnsSuccess()
    {
        var re = await _api.GetBangumiBySsid(46508, string.Empty);

        Assert.Equal(0, re.Code);
    }

    [Fact]
    public async Task GetVideoDetail_Normal_Success()
    {
        BiliApiResponse<VideoDetail> re = await _api.GetVideoDetail(
            new GetVideoDetailRequest { aid = "246364184" },
            _ck.ToString()
        );

        re.Code.Should().Be(0);
        re.Data.Should().NotBeNull();
        re.Data!.Aid.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRegionRankingVideosV2_ValidRequest_ReturnsSuccess()
    {
        BiliApiResponse<Ranking> re = await _api.GetRegionRankingVideosV2();

        Assert.Equal(0, re.Code);
    }

    #endregion

    #region 专栏

    [Fact]
    public async Task SearchUpArticlesByUpIdAsync_InputId_GetResultSuccess()
    {
        var mid = 1585227649;
        var req = new SearchArticlesByUpIdDto { mid = mid };
        await _wbiService.SetWridAsync(req, _ck);

        BiliApiResponse<SearchUpArticlesResponse> re = await _api.SearchUpArticlesByUpIdAsync(req);

        re.Code.Should().Be(0);
        re.Data!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchArticleInfoAsync_ValidId_GetResultSuccess()
    {
        var cvid = 34150576;

        var re = await _api.SearchArticleInfoAsync(cvid);

        re.Code.Should().Be(0);
        re.Data!.Mid.Should().BeGreaterThan(0);
        re.Data.Like.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SearchArticleInfoAsync_InvalidId_NoResult()
    {
        var cvid = 123;

        var re = await _api.SearchArticleInfoAsync(cvid);

        re.Code.Should().Be(-404);
        re.Data.Should().BeNull();
        re.Message.Should().BeEquivalentTo("啥都木有");
    }

    [Fact]
    public async Task AddCoinForArticleAsync_CoinSelf_Fail()
    {
        var selfCvId = 34150576; //todo
        var req = new AddCoinForArticleRequest(selfCvId, long.Parse(_ck.UserId), _ck.BiliJct);

        BiliApiResponse re = await _api.AddCoinForArticleAsync(req, _ck.ToString());

        re.Code.Should().Be(34002);
        re.Message.Should().BeEquivalentTo("up主不能自己投币");
    }

    [Fact]
    public async Task AddCoinForArticleAsync_Normal_Success()
    {
        var cvId = 34049005; //todo
        var upId = 25150765; //todo
        var req = new AddCoinForArticleRequest(cvId, upId, _ck.BiliJct);

        BiliApiResponse re = await _api.AddCoinForArticleAsync(req, _ck.ToString());

        re.Code.Should()
            .BeOneOf(
                0, // 成功
                34005 // 超过投币上限啦~
            );
    }

    [Fact]
    public async Task LikeAsync_AlreadyLike_GetResultSuccess()
    {
        var cvid = 34150576;

        var re = await _api.LikeAsync(cvid, _ck.BiliJct, _ck.ToString());

        re.Code.Should()
            .BeOneOf(
                new List<int>
                {
                    0,
                    65006, //已赞过
                }
            );
    }

    #endregion

    public void Dispose()
    {
        _scope.Dispose();
        _host.Dispose();
    }
}
