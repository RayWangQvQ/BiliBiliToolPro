using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class ArticleApiTests
{
    private readonly IArticleApi _api;

    private readonly BiliCookie _ck;
    private readonly IWbiService _wbiService;

    public ArticleApiTests()
    {
        var envs = new List<string>
        {
            "--ENVIRONMENT=Development",
            //"HTTP_PROXY=localhost:8888",
            //"HTTPS_PROXY=localhost:8888"
        };
        IHost host = Program.CreateHost(envs.ToArray());
        _ck = host.Services.GetRequiredService<BiliCookie>();
        _wbiService = host.Services.GetRequiredService<IWbiService>();
        _api = host.Services.GetRequiredService<IArticleApi>();
    }

    #region SearchUpArticlesByUpIdAsync

    [Fact]
    public async Task SearchUpArticlesByUpIdAsync_InputId_GetResultSuccess()
    {
        // Arrange
        var mid = 1585227649;
        var req = new SearchArticlesByUpIdDto()
        {
            mid = mid,
        };
        await _wbiService.SetWridAsync(req);

        // Act
        BiliApiResponse<SearchUpArticlesResponse> re = await _api.SearchUpArticlesByUpIdAsync(req);

        // Assert
        re.Code.Should().Be(0);
        re.Data.Count.Should().BeGreaterThan(0);
    }

    #endregion

    #region SearchArticleInfoAsync

    [Fact]
    public async Task SearchArticleInfoAsync_ValidId_GetResultSuccess()
    {
        // Arrange
        var cvid = 34150576;

        // Act
        var re = await _api.SearchArticleInfoAsync(cvid);

        // Assert
        re.Code.Should().Be(0);
        re.Data.Mid.Should().BeGreaterThan(0);
        re.Data.Like.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SearchArticleInfoAsync_InvalidId_NoResult()
    {
        // Arrange
        var cvid = 123;

        // Act
        var re = await _api.SearchArticleInfoAsync(cvid);

        // Assert
        re.Code.Should().Be(-404);
        re.Data.Should().IsNull();
        re.Message.Should().BeEquivalentTo("啥都木有");
    }

    #endregion


    #region AddCoinForArticleAsync

    [Fact]
    public async Task AddCoinForArticleAsync_CoinSelf_Fail()
    {
        // Arrange
        var selfCvId = 34150576;//todo
        var req = new AddCoinForArticleRequest(selfCvId, long.Parse(_ck.UserId), _ck.BiliJct);

        // Act
        BiliBiliAgent.Dtos.BiliApiResponse re = await _api.AddCoinForArticleAsync(req);

        // Assert
        re.Code.Should().Be(34002);
        re.Message.Should().BeEquivalentTo("up主不能自己投币");
    }

    [Fact]
    public async Task AddCoinForArticleAsync_Normal_Success()
    {
        // Arrange
        var cvId = 34049005;//todo
        var upId = 25150765;//todo
        var req = new AddCoinForArticleRequest(cvId, upId, _ck.BiliJct);

        // Act
        BiliBiliAgent.Dtos.BiliApiResponse re = await _api.AddCoinForArticleAsync(req);

        // Assert
        re.Code.Should().BeOneOf(
            0,// 成功
            34005 // 超过投币上限啦~
            );
    }

    #endregion

    #region LikeAsync

    [Fact]
    public async Task LikeAsync_AlreadyLike_GetResultSuccess()
    {
        // Arrange
        var cvid = 34150576;

        // Act
        var re = await _api.LikeAsync(cvid, _ck.BiliJct);

        // Assert
        re.Code.Should().BeOneOf(new List<int>
        {
            0,
            65006, //已赞过
        });
    }

    #endregion

}
