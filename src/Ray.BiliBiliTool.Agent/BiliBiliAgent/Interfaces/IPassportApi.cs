using System.Net.Http;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Header("Host", "passport.bilibili.com")]
    public interface IPassportApi : IBiliBiliApi
    {
        [HttpGet("/x/passport-login/web/qrcode/generate")]
        Task<BiliApiResponse<QrCodeDto>> GenerateQrCode();

        [HttpGet("/x/passport-login/web/qrcode/poll?qrcode_key={qrcode_key}&source=main_mini")]
        //Task<BiliApiResponse<TokenDto>> CheckQrCodeHasScaned(string qrcode_key);
        Task<HttpResponseMessage> CheckQrCodeHasScaned(string qrcode_key);

        [HttpGet("/x/passport-login/web/sso/list?biliCSRF={csrf}")]
        Task<BiliApiResponse<GetSsoListResponse>> GetSsoListAsync(string csrf);
    }
}
