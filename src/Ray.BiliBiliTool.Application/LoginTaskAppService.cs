using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Application
{
    public class LoginTaskAppService : AppService, ILoginTaskAppService
    {
        private readonly ILogger<LoginTaskAppService> _logger;
        private readonly IPassportApi _passportApi;
        private readonly IConfiguration _configuration;

        public LoginTaskAppService(
            IConfiguration configuration,
            ILogger<LoginTaskAppService> logger,
            IPassportApi passportApi
            )
        {
            _configuration = configuration;
            _logger = logger;
            _passportApi = passportApi;
        }

        [TaskInterceptor("扫码登录", TaskLevel.One)]
        public override void DoTask()
        {
            var re = _passportApi.GenerateQrCode().Result;
            if (re.Code != 0)
            {
                _logger.LogWarning("获取二维码失败：{msg}", re.ToJson());
                return;
            }

            var url = re.Data.Url;
            GenerateQrCode(url);

            var online = GetOnlinePic(url);
            _logger.LogInformation(Environment.NewLine + Environment.NewLine);
            _logger.LogInformation("如果上方二维码显示异常，或扫描失败，请使用浏览器访问如下链接，查看高清在线二维码：");
            _logger.LogInformation(online + Environment.NewLine + Environment.NewLine);


            for (int i = 0; i < 10; i++)
            {
                _logger.LogInformation("等待扫描...");

                Task.Delay(5 * 1000).Wait();

                var check = _passportApi.CheckQrCodeHasScaned(re.Data.Qrcode_key).Result;
                if (check.Code != 0)
                {
                    _logger.LogWarning("调用检查接口异常：{msg}", check.ToJson());
                    break;
                }

                if (check.Data.Code == 86101)//未扫描
                {
                    _logger.LogInformation("[{num}]：{msg}", i + 1, check.Data.Message + Environment.NewLine);
                    continue;
                }

                if (check.Data.Code == 86038)//已失效
                {
                    _logger.LogInformation(check.Data.Message);
                    break;
                }

                if (check.Data.Code == 0)
                {
                    _logger.LogInformation("扫描成功！");
                    _logger.LogInformation(check.Data.Url);
                    break;
                }
            }
        }

        private void GenerateQrCode(string str)
        {
            var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(str, QRCodeGenerator.ECCLevel.L);

            _logger.LogInformation("AsciiQRCode：");
            var qrCode = new AsciiQRCode(qrCodeData);
            //var qrCodeStr = qrCode.GetGraphic(1, drawQuietZones: false);
            //_logger.LogInformation(Environment.NewLine + qrCodeStr);

            Console.WriteLine("Console：");
            //Print(qrCodeData);
            PrintSmall(qrCodeData);
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

        private void PrintSmall(QRCodeData qrCodeData)
        {
            //黑黑（" "）
            //白白（"█"）
            //黑白（"▄"）
            //白黑（"▀"）
            var dic = new Dictionary<string, string>()
            {
                {"11", " "},
                {"00", "█"},
                {"10", "▄"},
                {"01", "▀"},
            };

            var count = qrCodeData.ModuleMatrix.Count;

            var list = new List<List<string>>();
            for (int rowNum = 0; rowNum < count; rowNum++)
            {
                var rowStr = new List<string>();
                for (int colNum = 0; colNum < count; colNum++)
                {
                    var num = qrCodeData.ModuleMatrix[colNum][rowNum] ? "1" : "0";
                    var numDown = "1";
                    if (rowNum + 1 < count)
                        numDown = qrCodeData.ModuleMatrix[colNum][rowNum + 1] ? "1" : "0";

                    rowStr.Add(dic[num + numDown]);
                }

                _logger.LogInformation(string.Join("", rowStr));

                rowNum++;
                list.Add(rowStr);
            }

            _logger.LogInformation("Console：");
            foreach (var row in list)
            {
                Console.WriteLine(string.Join("", row));
            }
            Console.WriteLine();
        }

        private string GetOnlinePic(string str)
        {
            var encode = System.Web.HttpUtility.UrlEncode(str); ;
            return $"https://tool.lu/qrcode/basic.html?text={encode}";
        }
    }
}
