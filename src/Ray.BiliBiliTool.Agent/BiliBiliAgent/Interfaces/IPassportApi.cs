using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.PassportApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Headers("Host: passport.bilibili.com")]
public interface IPassportApi
{
    [Get("/x/passport-login/web/qrcode/generate")]
    Task<BiliApiResponse<QrCodeDto>> GenerateQrCode();

    [Get("/x/passport-login/web/qrcode/poll?qrcode_key={qrcode_key}&source=main_mini")]
    //Task<BiliApiResponse<TokenDto>> CheckQrCodeHasScaned(string qrcode_key);
    Task<HttpResponseMessage> CheckQrCodeHasScaned(string qrcode_key);

    [Get("/x/passport-login/web/sso/list?biliCSRF={csrf}")]
    Task<BiliApiResponse<GetSsoListResponse>> GetSsoListAsync(string csrf);
}
