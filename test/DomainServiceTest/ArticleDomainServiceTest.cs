﻿using Xunit.Abstractions;

namespace DomainServiceTest;

public class ArticleDomainServiceTest
{
    private readonly ITestOutputHelper _output;
    public ArticleDomainServiceTest(ITestOutputHelper output)
    {
        _output = output;
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public async Task CalculateDonateCoinsCountsTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var config = Global.ConfigurationRoot;
        var domainService = scope.ServiceProvider.GetRequiredService<IArticleDomainService>();

        int coins = await domainService.CalculateDonateCoinsCounts();
        _output.WriteLine($"当前测试账户应当投币{coins}枚，请根据账户硬币余额自行计算结果是否正确");
    }



    [Fact]
    public async Task AddCoinForArticleTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var config = Global.ConfigurationRoot;
        var domainService = scope.ServiceProvider.GetRequiredService<IArticleDomainService>();

        // 测试用的专栏：https://www.bilibili.com/read/cv5806746/?from=search&spm_id_from=333.337.0.0

        await domainService.AddCoinForArticle(5806746, 486980924);
    }


    [Fact]
    public async Task AddCoinForArticlesTest()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var config = Global.ConfigurationRoot;
        var domainService = scope.ServiceProvider.GetRequiredService<IArticleDomainService>();
        await domainService.AddCoinForArticles();
    }
}
