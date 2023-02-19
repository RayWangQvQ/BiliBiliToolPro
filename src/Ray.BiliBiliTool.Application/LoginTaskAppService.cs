using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure.Enums;
using System.Threading;

namespace Ray.BiliBiliTool.Application
{
    public class LoginTaskAppService : AppService, ILoginTaskAppService
    {
        private readonly ILogger<LoginTaskAppService> _logger;
        private readonly IPassportApi _passportApi;
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IQingLongApi _qingLongApi;
        private readonly IConfiguration _configuration;

        public LoginTaskAppService(
            IConfiguration configuration,
            ILogger<LoginTaskAppService> logger,
            IPassportApi passportApi,
            IHostEnvironment hostingEnvironment,
            IQingLongApi qingLongApi)
        {
            _configuration = configuration;
            _logger = logger;
            _passportApi = passportApi;
            _hostingEnvironment = hostingEnvironment;
            _qingLongApi = qingLongApi;
        }

        [TaskInterceptor("扫码登录", TaskLevel.One)]
        public override async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            //扫码登录
            var cookieInfo = await QrCodeLoginAsync(cancellationToken);
            if (cookieInfo == null) return;

            var plateformType = _configuration.GetSection("PlateformType").Get<PlateformType>();

            //更新cookie到青龙env
            if (plateformType == PlateformType.QingLong)
            {
                await AddOrUpdateCkToQingLong(cookieInfo);
                return;
            }

            //更新cookie到json
            await AddOrUpdateCkToJson(cookieInfo);
        }

        [TaskInterceptor("二维码登录", TaskLevel.Two)]
        protected async Task<BiliCookie> QrCodeLoginAsync(CancellationToken cancellationToken)
        {
            BiliCookie cookieInfo = null;

            var re = await _passportApi.GenerateQrCode();
            if (re.Code != 0)
            {
                _logger.LogWarning("获取二维码失败：{msg}", re.ToJson());
                return null;
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

                await Task.Delay(5 * 1000, cancellationToken);

                var check = await _passportApi.CheckQrCodeHasScaned(re.Data.Qrcode_key);
                if (!check.IsSuccessStatusCode)
                {
                    _logger.LogWarning("调用检测接口异常");
                    continue;
                }

                var content = JsonConvert.DeserializeObject<BiliApiResponse<TokenDto>>(await check.Content.ReadAsStringAsync(cancellationToken));
                if (content.Code != 0)
                {
                    _logger.LogWarning("调用检测接口异常：{msg}", check.ToJson());
                    break;
                }

                if (content.Data.Code == 86038)//已失效
                {
                    _logger.LogInformation(content.Data.Message);
                    break;
                }

                if (content.Data.Code == 0)
                {
                    _logger.LogInformation("扫描成功！");
                    IEnumerable<string> cookies = check.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;

                    cookieInfo = GetCookie(cookies);

                    break;
                }

                _logger.LogInformation("{msg}", content.Data.Message + Environment.NewLine);
            }

            return cookieInfo;
        }


        [TaskInterceptor("添加ck到json配置文件", TaskLevel.Two)]
        protected async Task AddOrUpdateCkToJson(BiliCookie ckInfo)
        {
            //读取json
            var path = _hostingEnvironment.ContentRootPath;
            var indexOfBin = path.LastIndexOf("bin");
            if (indexOfBin != -1) path = path[..indexOfBin];
            var fileProvider = new PhysicalFileProvider(path);
            IFileInfo fileInfo = fileProvider.GetFileInfo("cookies.json");
            _logger.LogInformation("目标json地址：{path}", fileInfo.PhysicalPath);

            if (!fileInfo.Exists)
            {
                await using var stream = File.Create(fileInfo.PhysicalPath);
                await using var sw = new StreamWriter(stream);
                await sw.WriteAsync($"{{{Environment.NewLine}}}");
            }

            string json;
            await using (var stream = new FileStream(fileInfo.PhysicalPath, FileMode.Open))
            {
                using var reader = new StreamReader(stream);
                json = await reader.ReadToEndAsync();
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

                await SaveJson(lines, fileInfo);
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
                await SaveJson(lines, fileInfo);
                _logger.LogInformation("新增成功！");
                return;
            }

            _logger.LogInformation("已存在该用户，更新cookie");
            lines[indexOfTargetCk] = $@"    ""{ckInfo.CookieStr}"",";
            await SaveJson(lines, fileInfo);
            _logger.LogInformation("更新成功！");
        }

        [TaskInterceptor("添加ck到青龙环境变量", TaskLevel.Two)]
        protected async Task AddOrUpdateCkToQingLong(BiliCookie ckInfo)
        {
            //拿token
            var token = await GetQingLongAuthTokenAsync();

            if (token.IsNullOrEmpty()) return;

            token = $"Bearer {token}";

            //查env
            var re = await _qingLongApi.GetEnvs("Ray_BiliBiliCookies__", token);

            if (re.Code != 200)
            {
                _logger.LogInformation($"查询环境变量失败：{re}", re.ToJson());
                return;
            }

            var list = re.Data.Where(x => x.name.StartsWith("Ray_BiliBiliCookies__")).ToList();
            QingLongEnv oldEnv = list.FirstOrDefault(x => x.value.Contains(ckInfo.UserId));

            if (oldEnv != null)
            {
                _logger.LogInformation("用户已存在，更新cookie");
                _logger.LogInformation("Key：{key}", oldEnv.name);
                var update = new UpdateQingLongEnv()
                {
                    id = oldEnv.id,
                    name = oldEnv.name,
                    value = ckInfo.CookieStr,
                    remarks = oldEnv.remarks.IsNullOrEmpty()
                        ? $"bili-{ckInfo.UserId}"
                        : oldEnv.remarks,
                };

                var updateRe = await _qingLongApi.UpdateEnvs(update, token);
                _logger.LogInformation(updateRe.Code == 200 ? "更新成功！" : updateRe.ToJson());

                return;
            }

            _logger.LogInformation("用户不存在，新增cookie");
            var maxNum = -1;
            if (list.Any())
            {
                maxNum = list.Select(x =>
                {
                    var num = x.name.Replace("Ray_BiliBiliCookies__", "");
                    var parseSuc = int.TryParse(num, out int envNum);
                    return parseSuc ? envNum : 0;
                }).Max();
            }
            var name = $"Ray_BiliBiliCookies__{maxNum + 1}";
            _logger.LogInformation("Key：{key}", name);

            var add = new AddQingLongEnv()
            {
                name = name,
                value = ckInfo.CookieStr,
                remarks = $"bili-{ckInfo.UserId}"
            };
            var addRe = await _qingLongApi.AddEnvs(new List<AddQingLongEnv> { add }, token);
            _logger.LogInformation(addRe.Code == 200 ? "新增成功！" : addRe.ToJson());
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

        private BiliCookie GetCookie(IEnumerable<string> cookies)
        {
            var ckItemList = new List<string>();
            foreach (var item in cookies)
            {
                ckItemList.Add(item.Split(';').FirstOrDefault());
            }

            var biliCk = new BiliCookie(string.Join("; ", ckItemList));

            biliCk.Check();
            return biliCk;
        }

        private async Task SaveJson(List<string> lines, IFileInfo fileInfo)
        {
            var newJson = string.Join(Environment.NewLine, lines);

            await using var sw = new StreamWriter(fileInfo.PhysicalPath);
            await sw.WriteAsync(newJson);
        }

        #region qinglong

        private async Task<string> GetQingLongAuthTokenAsync()
        {
            var token = "";

            var qlDir = _configuration["QL_DIR"] ?? "/ql";

            string authFile = qlDir;
            if (_hostingEnvironment.ContentRootPath.Contains($"{qlDir}/data/"))
            {
                authFile = Path.Combine(authFile, "data");
            }
            authFile = Path.Combine(authFile, "config/auth.json");

            if (!File.Exists(authFile))
            {
                _logger.LogWarning("获取青龙授权失败，文件不在：{authFile}", authFile);
                return token;
            }

            var authJson = await File.ReadAllTextAsync(authFile);

            var jb = JsonConvert.DeserializeObject<JObject>(authJson);
            token = jb["token"]?.ToString();

            return token;
        }

        #endregion
    }
}
