using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QRCoder;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Enums;

namespace Ray.BiliBiliTool.Application
{
    public class LoginTaskAppService : AppService, ILoginTaskAppService
    {
        private readonly ILogger<LoginTaskAppService> _logger;
        private readonly IConfiguration _configuration;

        public LoginTaskAppService(
            IConfiguration configuration,
            ILogger<LoginTaskAppService> logger
            )
        {
            _configuration = configuration;
            _logger = logger;
        }

        [TaskInterceptor("扫码登录", TaskLevel.One)]
        public override void DoTask()
        {
            var url = "https://passport.bilibili.com/h5-app/passport/login/scan?navhide=1&qrcode_key=929388ca6339f01b725c415f0ba0ef5b&from=";
            GenerateQrCode(url);
        }

        private void GenerateQrCode(string str)
        {
            var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(str, QRCodeGenerator.ECCLevel.L);

            _logger.LogInformation("Graphic：");
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeStr = qrCode.GetGraphic(1);
            _logger.LogInformation(qrCodeStr);

            Console.WriteLine("Console：");
            Print(qrCodeData);
        }

        private void Print(QRCodeData qrCodeData)
        {
            Console.BackgroundColor = ConsoleColor.White;
            for (int i = 0; i < qrCodeData.ModuleMatrix.Count + 2; i++) Console.Write("　");//中文全角的空格符
            Console.WriteLine();
            for (int j = 0; j < qrCodeData.ModuleMatrix.Count; j++)
            {
                for (int i = 0; i < qrCodeData.ModuleMatrix.Count; i++)
                {
                    //char charToPoint = qrCode.Matrix[i, j] ? '█' : '　';
                    Console.Write(i == 0 ? "　" : "");//中文全角的空格符
                    Console.BackgroundColor = qrCodeData.ModuleMatrix[i][j] ? ConsoleColor.Black : ConsoleColor.White;
                    Console.Write('　');//中文全角的空格符
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.Write(i == qrCodeData.ModuleMatrix.Count - 1 ? "　" : "");//中文全角的空格符
                }
                Console.WriteLine();
            }
            for (int i = 0; i < qrCodeData.ModuleMatrix.Count + 2; i++) Console.Write("　");//中文全角的空格符

            Console.WriteLine();
        }
    }
}
