using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Console;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

public class ArticleApiTests
{
    private readonly IArticleApi _api;

    private readonly BiliCookie _ck;

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
        _api = host.Services.GetRequiredService<IArticleApi>();
    }

    #region SearchUpArticlesByUpIdAsync

    [Fact]
    public async Task SearchUpArticlesByUpIdAsync_CoinSelf_Fail()
    {
        // Arrange
        var mid = 1585227649;//todo
        var req = new SearchArticlesByUpIdDto()
        {
            mid = mid,
        };

        // Act
        BiliApiResponse<SearchUpArticlesResponse> re = await _api.SearchUpArticlesByUpIdAsync(req);

        // Assert
        re.Data.Count.Should().BeGreaterThan(0);
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
        re.Code.Should().Be(0);
        re.Message.Should().BeEquivalentTo("0");
    }

    #endregion

}
