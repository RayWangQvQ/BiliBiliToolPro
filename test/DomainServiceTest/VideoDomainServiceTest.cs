using Ray.BiliBiliTool.Infrastructure.Helpers;
using Ray.Infrastructure.Helpers;

namespace DomainServiceTest
{
    public class VideoDomainServiceTest
    {
        public VideoDomainServiceTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public async Task GetVideoCountOfUp_Test()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var config = Global.ConfigurationRoot;
            var domainService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

            await domainService.GetVideoCountOfUp(1585227649, null);
        }

        [Fact]
        public async Task GetRandomVideoOfUp_Test()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var config = Global.ConfigurationRoot;
            var domainService = scope.ServiceProvider.GetRequiredService<IVideoDomainService>();

            await domainService.GetRandomVideoOfUp(1585227649, 1, null);
        }
    }
}
