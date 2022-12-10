using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Application
{
    public class LoginTaskAppService : AppService, ILoginTaskAppService
    {
        private readonly ILogger<LoginTaskAppService> _logger;
        private readonly IPassportApi _passportApi;
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;

        public LoginTaskAppService(
            IConfiguration configuration,
            ILogger<LoginTaskAppService> logger,
            IPassportApi passportApi,
            IHostEnvironment hostingEnvironment
            )
        {
            _configuration = configuration;
            _logger = logger;
            _passportApi = passportApi;
            _hostingEnvironment = hostingEnvironment;
        }

        [TaskInterceptor("扫码登录", TaskLevel.One)]
        public override void DoTask()
        {
            //扫码登录
            var suc = QrCodeLogin(out BiliCookie cookieInfo);
            if (!suc) return;

            //更新cookie到json
            AddOrUpdateCkToJson(cookieInfo);

            //更新cookie到青龙env
            //todo
        }

        [TaskInterceptor("二维码登录", TaskLevel.Two)]
        protected bool QrCodeLogin(out BiliCookie cookieInfo)
        {
            var result = false;
            cookieInfo = new BiliCookie(new List<string>(){""});

            var re = _passportApi.GenerateQrCode().Result;
            if (re.Code != 0)
            {
                _logger.LogWarning("获取二维码失败：{msg}", re.ToJson());
                return result;
            }

            var url = re.Data.Url;
            GenerateQrCode(url);

            var online = GetOnlinePic(url);
            _logger.LogInformation(Environment.NewLine + Environment.NewLine);
            _logger.LogInformation("如果上方二维码显示异常，或扫描失败，请使用浏览器访问如下链接，查看高清二维码：");
            _logger.LogInformation(online + Environment.NewLine + Environment.NewLine);

            var waitTimes = 10;
            _logger.LogInformation("我数到{num}，动作快点", waitTimes);
            for (int i = 0; i < waitTimes; i++)
            {
                _logger.LogInformation("[{num}]等待扫描...", i + 1);

                Task.Delay(5 * 1000).Wait();

                var check = _passportApi.CheckQrCodeHasScaned(re.Data.Qrcode_key).Result;
                if (check.Code != 0)
                {
                    _logger.LogWarning("调用检查接口异常：{msg}", check.ToJson());
                    break;
                }

                if (check.Data.Code == 86038)//已失效
                {
                    _logger.LogInformation(check.Data.Message);
                    break;
                }

                if (check.Data.Code == 0)
                {
                    _logger.LogInformation("扫描成功！");
                    cookieInfo = GetCookie(check.Data.Url);
                    result = true;
                    break;
                }

                _logger.LogInformation("{msg}", check.Data.Message + Environment.NewLine);
            }

            return result;
        }


        [TaskInterceptor("添加ck到json配置文件", TaskLevel.Two)]
        protected void AddOrUpdateCkToJson(CookieInfo ckInfo)
        {
            //读取json
            var path = _hostingEnvironment.ContentRootPath;
            var indexOfBin = path.LastIndexOf("bin");
            if (indexOfBin != -1) path = path.Substring(0, indexOfBin);
            var fileProvider = new PhysicalFileProvider(path);
            IFileInfo fileInfo = fileProvider.GetFileInfo("appsettings.json");
            _logger.LogInformation("目标json地址：{path}", fileInfo.PhysicalPath);

            string json;
            using (var stream = new FileStream(fileInfo.PhysicalPath, FileMode.Open))
            {
                using var reader = new StreamReader(stream);
                json = reader.ReadToEnd();
            }
            var lines = json.Split(Environment.NewLine).ToList();

            var indexOfCkConfigKey = lines.FindIndex(x => x.TrimStart().StartsWith("\"BiliBiliCookies\""));
            if (indexOfCkConfigKey == -1)
            {
                _logger.LogInformation("未配置过cookie，初始化并新增");

                var indexOfInsert = lines.FindIndex(x => x.TrimStart().StartsWith("{"));
                lines.InsertRange(indexOfInsert + 1, new List<string>()
                {
                    "  \"BiliBiliCookies\":[",
                    $@"    ""{ckInfo.CookieStr}"",",
                    "  ],"
                });

                SaveJson(lines, fileInfo);
                _logger.LogInformation("新增成功！");
                return;
            }

            ckInfo.CookieItemDictionary.TryGetValue("DedeUserID", out string userId);
            userId ??= ckInfo.CookieStr;
            var indexOfCkConfigEnd = lines.FindIndex(indexOfCkConfigKey, x => x.TrimStart().StartsWith("]"));
            var indexOfTargetCk = lines.FindIndex(indexOfCkConfigKey,
                indexOfCkConfigEnd - indexOfCkConfigKey,
                x => x.Contains(userId) && !x.TrimStart().StartsWith("//"));

            if (indexOfTargetCk == -1)
            {
                _logger.LogInformation("不存在该用户，新增cookie");
                lines.Insert(indexOfCkConfigEnd, $@"    ""{ckInfo.CookieStr}"",");
                SaveJson(lines, fileInfo);
                _logger.LogInformation("新增成功！");
                return;
            }

            _logger.LogInformation("已存在该用户，更新cookie");
            lines[indexOfTargetCk] = $@"    ""{ckInfo.CookieStr}"",";
            SaveJson(lines, fileInfo);
            _logger.LogInformation("更新成功！");
        }

        private void GenerateQrCode(string str)
        {
            var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(str, QRCodeGenerator.ECCLevel.L);

            _logger.LogInformation("AsciiQRCode：");
            //var qrCode = new AsciiQRCode(qrCodeData);
            //var qrCodeStr = qrCode.GetGraphic(1, drawQuietZones: false);
            //_logger.LogInformation(Environment.NewLine + qrCodeStr);

            //Console.WriteLine("Console：");
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
            var dic = new Dictionary<string, char>()
            {
                {"11", ' '},
                {"00", '█'},
                {"10", '▄'},
                {"01", '▀'},//todo:win平台的cmd会显示？,是已知问题，待想办法解决
                //{"01", '^'},//▼▔
            };

            var count = qrCodeData.ModuleMatrix.Count;

            var list = new List<string>();
            for (int rowNum = 0; rowNum < count; rowNum++)
            {
                var rowStr = "";
                for (int colNum = 0; colNum < count; colNum++)
                {
                    var num = qrCodeData.ModuleMatrix[colNum][rowNum] ? "1" : "0";
                    var numDown = "0";
                    if (rowNum + 1 < count)
                        numDown = qrCodeData.ModuleMatrix[colNum][rowNum + 1] ? "1" : "0";

                    rowStr += dic[num + numDown];
                }
                list.Add(rowStr);
                rowNum++;
            }

            _logger.LogInformation(Environment.NewLine + string.Join(Environment.NewLine, list));
        }

        private string GetOnlinePic(string str)
        {
            var encode = System.Web.HttpUtility.UrlEncode(str); ;
            return $"https://tool.lu/qrcode/basic.html?text={encode}";
        }

        private BiliCookie GetCookie(string url)
        {
            var ckItemList = url.Split('?')[1]
                .Split("&gourl=")[0]
                .Split('&')
                .ToList();
            var ckStr = string.Join(';', ckItemList);
            var biliCk = new BiliCookie(new List<string>() { ckStr });

            biliCk.Check();
            return biliCk;
        }

        private void SaveJson(List<string> lines, IFileInfo fileInfo)
        {
            var newJson = string.Join(Environment.NewLine, lines);

            using (var sw = new StreamWriter(fileInfo.PhysicalPath))
            {
                sw.Write(newJson);
            }
        }
    }
}
