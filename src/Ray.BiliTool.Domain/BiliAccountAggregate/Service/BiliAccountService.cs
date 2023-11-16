using Ray.BiliTool.Domain.BiliAccountAggregate.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport;
using Ray.BiliTool.Domain.BiliAccountAggregate.Entity;
using Ray.DDD;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using ZXing.QrCode.Internal;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.Infrastructure.BarCode;

namespace Ray.BiliTool.Domain.BiliAccountAggregate.Service
{
    public class BiliAccountService: IDomainService
    {
        private readonly IBiliAccountRepository _repository;
        private readonly IPassportApi _passportApi;

        public BiliAccountService(
            IBiliAccountRepository repository,
            IPassportApi passportApi
            )
        {
            _repository = repository;
            _passportApi = passportApi;
        }

        public async Task<LoginQrCode> GenerateLoginQrCodeAsync()
        {
            BiliApiResponse<QrCodeDto> re = await _passportApi.GenerateQrCode();
            if (re.Code != 0)
            {
                ModalService.Error(new ConfirmOptions()
                {
                    Title = "获取二维码失败",
                    Content = re.ToJsonStr()
                });
                return;
            }
            var url = re.Data.Url;

            Image<Rgba32>? bitmap = BarCodeHelper.EncodeByImageSharp(url);

            _qrCode = bitmap.ToBase64String(PngFormat.Instance);
            _qrCodeKey = re.Data.Qrcode_key;
        }

        public void LoginByScan()
        {

        }
    }
}
