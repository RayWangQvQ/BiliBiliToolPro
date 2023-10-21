namespace DomainServiceTest;

public class ArticleDomainServiceTest
{
    public ArticleDomainServiceTest()
    {
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
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
}
