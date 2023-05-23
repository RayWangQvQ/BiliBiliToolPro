
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Video;
using Ray.BiliBiliTool.Infrastructure.Helpers;
using Ray.Infrastructure.Helpers;

namespace DomainServiceTest
{
    public class WbiDomainServiceTest
    {
        public WbiDomainServiceTest()
        {
            Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
        }

        [Fact]
        public async Task EncWbi_Test()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var config = Global.ConfigurationRoot;
            var domainService = scope.ServiceProvider.GetRequiredService<IWbiDomainService>();

            var upId = 1585227649;

            var req = new SearchVideosByUpIdDto()
            {
                mid = upId,
                ps = 30,
                tid = 0,
                pn = 1,
                keyword = "",
                order = "pubdate",
                platform = "web",
                web_location = 1550101,
                order_avoided = "true"
            };

            //var wbiDto= await domainService.GetWbiKeysAsync();
            WbiImg wbiDto= new WbiImg()
            {
                img_url = "https://i0.hdslb.com/bfs/wbi/9cd4224d4fe74c7e9d6963e2ef891688.png",
                sub_url = "https://i0.hdslb.com/bfs/wbi/263655ae2cad4cce95c9c401981b044a.png"
            };
            var dic = ObjectHelper.ObjectToDictionary(req);
            //dic.Remove("keyword");
            var re = domainService.EncWbi(dic, wbiDto.GetImgKey(),wbiDto.GetSubKey(), 1684866934);

            Assert.Equal(re.w_rid, "8dca01c5633c1ed8cda9566b8502ca03");
        }

        [Fact]
        public async Task EncWbi_Test2()
        {
            using var scope = Global.ServiceProviderRoot.CreateScope();
            var domainService = scope.ServiceProvider.GetRequiredService<IWbiDomainService>();

            WbiImg wbiDto = new WbiImg()
            {
                img_url= "https://i0.hdslb.com/bfs/wbi/653657f524a547ac981ded72ea172057.png",
                sub_url = "https://i0.hdslb.com/bfs/wbi/6e4909c702f846728e64f6007736a338.png"
            };
            var dic = new Dictionary<string, object>()
            {
                {"foo", "114"},
                {"bar", "514"},
                {"baz", "1919810"},
            };
            var re = domainService.EncWbi(dic, wbiDto.GetImgKey(), wbiDto.GetSubKey(), 1684746387);

            Assert.Equal(re.w_rid, "d3cbd2a2316089117134038bf4caf442");
        }
    }
}
