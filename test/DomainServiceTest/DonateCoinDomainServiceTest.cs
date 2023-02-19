
namespace DomainServiceTest
{
    public class DonateCoinDomainServiceTest
    {
        public DonateCoinDomainServiceTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public async Task AddCoinsForVideos()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var config = Global.ConfigurationRoot;
            var domainService = scope.ServiceProvider.GetRequiredService<IDonateCoinDomainService>();

            await domainService.AddCoinsForVideos();
        }
    }
}
