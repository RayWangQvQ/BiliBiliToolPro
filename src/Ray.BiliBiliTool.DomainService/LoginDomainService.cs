using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 账户
/// </summary>
public class LoginDomainService(
    ILogger<LoginDomainService> logger,
    IPassportApi passportApi,
    IHostEnvironment hostingEnvironment,
    IQingLongApi qingLongApi,
    IHomeApi homeApi,
    IConfiguration configuration
) : ILoginDomainService
{
    public async Task<BiliCookie> LoginByQrCodeAsync(CancellationToken cancellationToken)
    {
        BiliCookie cookieInfo = null;

        var re = await passportApi.GenerateQrCode();
        if (re.Code != 0)
        {
            logger.LogWarning("获取二维码失败：{msg}", re.ToJsonStr());
            return null;
        }

        var url = re.Data.Url;
        GenerateQrCode(url);

        var online = GetOnlinePic(url);
        logger.LogInformation(Environment.NewLine + Environment.NewLine);
        logger.LogInformation(
            "如果上方二维码显示异常，或扫描失败，请使用浏览器访问如下链接，查看高清二维码："
        );
        logger.LogInformation(online + Environment.NewLine + Environment.NewLine);

        var waitTimes = 10;
        logger.LogInformation("我数到{num}，动作快点", waitTimes);
        for (int i = 0; i < waitTimes; i++)
        {
            logger.LogInformation("[{num}]等待扫描...", i + 1);

            await Task.Delay(5 * 1000, cancellationToken);

            var check = await passportApi.CheckQrCodeHasScaned(re.Data.Qrcode_key);
            if (!check.IsSuccessStatusCode)
            {
                logger.LogWarning("调用检测接口异常");
                continue;
            }

            var contentStr = await check.Content.ReadAsStringAsync(cancellationToken);
            var content = JsonConvert.DeserializeObject<BiliApiResponse<TokenDto>>(contentStr);
            if (content.Code != 0)
            {
                logger.LogWarning("调用检测接口异常：{msg}", check.ToJsonStr());
                break;
            }

            if (content.Data.Code == 86038) //已失效
            {
                logger.LogInformation(content.Data.Message);
                break;
            }

            if (content.Data.Code == 0)
            {
                logger.LogInformation("扫描成功！");
                IEnumerable<string> cookies = check
                    .Headers.SingleOrDefault(header => header.Key == "Set-Cookie")
                    .Value;

                var cookieStr = CookieInfo.ConvertSetCkHeadersToCkStr(cookies);

                cookieInfo = CookieStrFactory<BiliCookie>.CreateNew(cookieStr);
                cookieInfo.Check();

                break;
            }

            logger.LogInformation("{msg}", content.Data.Message + Environment.NewLine);
        }

        return cookieInfo;
    }

    public async Task<BiliCookie> SetCookieAsync(
        BiliCookie biliCookie,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var homePage = await homeApi.GetHomePageAsync(biliCookie.ToString());
            if (homePage.IsSuccessStatusCode)
            {
                logger.LogInformation("访问主站成功");
                IEnumerable<string> setCookieHeaders = homePage
                    .Headers.SingleOrDefault(header => header.Key == "Set-Cookie")
                    .Value;
                if (setCookieHeaders != null)
                {
                    biliCookie.MergeCurrentCookieBySetCookieHeaders(setCookieHeaders);
                    logger.LogInformation("SetCookie成功");
                }
                else
                {
                    logger.LogInformation("无需set");
                }

                return biliCookie;
            }
            logger.LogError("访问主站失败：{msg}", homePage.ToJsonStr());
        }
        catch (Exception e)
        {
            //buvid只影响分享和投币，可以吞掉异常
            logger.LogError(e.ToJsonStr());
        }

        return biliCookie;
    }

    public async Task SaveCookieToJsonFileAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        //读取json
        var path = hostingEnvironment.ContentRootPath;
        var indexOfBin = path.LastIndexOf("bin");
        if (indexOfBin != -1)
        {
            path = path[..indexOfBin];
        }
        if (string.Equals(configuration["PlatformType"], "Web", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(path, "config");
        }
        var fileProvider = new PhysicalFileProvider(path);
        IFileInfo fileInfo = fileProvider.GetFileInfo("cookies.json");
        logger.LogInformation("目标json地址：{path}", fileInfo.PhysicalPath);

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

        var indexOfCkConfigKey = lines.FindIndex(x =>
            x.TrimStart().StartsWith("\"BiliBiliCookies\"")
        );
        if (indexOfCkConfigKey == -1)
        {
            logger.LogInformation("未配置过cookie，初始化并新增");

            var indexOfInsert = lines.FindIndex(x => x.TrimStart().StartsWith("{"));
            lines.InsertRange(
                indexOfInsert + 1,
                new List<string>()
                {
                    "  \"BiliBiliCookies\":[",
                    $@"    ""{ckInfo.CookieStr}"",",
                    "  ],",
                }
            );

            await SaveJson(lines, fileInfo);
            logger.LogInformation("新增成功！");
            return;
        }

        ckInfo.CookieItemDictionary.TryGetValue("DedeUserID", out string userId);
        userId ??= ckInfo.CookieStr;
        var indexOfCkConfigEnd = lines.FindIndex(
            indexOfCkConfigKey,
            x => x.TrimStart().StartsWith("]")
        );
        var indexOfTargetCk = lines.FindIndex(
            indexOfCkConfigKey,
            indexOfCkConfigEnd - indexOfCkConfigKey,
            x => x.Contains(userId) && !x.TrimStart().StartsWith("//")
        );

        if (indexOfTargetCk == -1)
        {
            logger.LogInformation("不存在该用户，新增cookie");
            lines.Insert(indexOfCkConfigEnd, $@"    ""{ckInfo.CookieStr}"",");
            await SaveJson(lines, fileInfo);
            logger.LogInformation("新增成功！");
            return;
        }

        logger.LogInformation("已存在该用户，更新cookie");
        lines[indexOfTargetCk] = $@"    ""{ckInfo.CookieStr}"",";
        await SaveJson(lines, fileInfo);
        logger.LogInformation("更新成功！");
    }

    public async Task<bool> SaveCookieToQinLongAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var token = await GetQingLongAuthTokenAsync();
            if (token.IsNullOrEmpty())
            {
                throw new Exception("获取青龙token失败");
            }

            token = $"Bearer {token}";

            var qlEnvList = await qingLongApi.GetEnvs("Ray_BiliBiliCookies__", token);
            if (qlEnvList.Code != 200)
            {
                throw new Exception($"查询环境变量失败：{qlEnvList.ToJsonStr()}");
            }

            logger.LogDebug(qlEnvList.Data.ToJsonStr());
            logger.LogDebug(ckInfo.ToString());

            var list = qlEnvList
                .Data.Where(x => x.name.StartsWith("Ray_BiliBiliCookies__"))
                .ToList();
            QingLongEnv oldEnv = list.FirstOrDefault(x => x.value.Contains(ckInfo.UserId));

            if (oldEnv != null)
            {
                logger.LogInformation("用户已存在，更新cookie");
                logger.LogInformation("Key：{key}", oldEnv.name);
                var update = new UpdateQingLongEnv
                {
                    id = oldEnv.id,
                    name = oldEnv.name,
                    value = ckInfo.CookieStr,
                    remarks = oldEnv.remarks.IsNullOrEmpty()
                        ? $"bili-{ckInfo.UserId}"
                        : oldEnv.remarks,
                };

                var updateRe = await qingLongApi.UpdateEnvs(update, token);
                logger.LogInformation(updateRe.Code == 200 ? "更新成功！" : updateRe.ToJsonStr());

                return true;
            }

            logger.LogInformation("用户不存在，新增cookie");
            var maxNum = -1;
            if (list.Any())
            {
                maxNum = list.Select(x =>
                    {
                        var num = x.name.Replace("Ray_BiliBiliCookies__", "");
                        var parseSuc = int.TryParse(num, out int envNum);
                        return parseSuc ? envNum : 0;
                    })
                    .Max();
            }

            var name = $"Ray_BiliBiliCookies__{maxNum + 1}";
            logger.LogInformation("Key：{key}", name);

            var add = new AddQingLongEnv
            {
                name = name,
                value = ckInfo.CookieStr,
                remarks = $"bili-{ckInfo.UserId}",
            };
            var addRe = await qingLongApi.AddEnvs([add], token);
            logger.LogInformation(addRe.Code == 200 ? "新增成功！" : addRe.ToJsonStr());
            return true;
        }
        catch
        {
            await PrintIfSaveCookieFailAsync(ckInfo, cancellationToken);
            return false;
        }
    }

    #region private

    private void GenerateQrCode(string str)
    {
        var qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(str, QRCodeGenerator.ECCLevel.L);

        logger.LogInformation("AsciiQRCode：");
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
        for (int i = 0; i < qrCodeData.ModuleMatrix.Count + 2; i++)
            Console.Write("　"); //中文全角的空格符
        Console.WriteLine();
        for (int j = 0; j < qrCodeData.ModuleMatrix.Count; j++)
        {
            for (int i = 0; i < qrCodeData.ModuleMatrix.Count; i++)
            {
                //char charToPoint = qrCode.Matrix[i, j] ? '█' : '　';
                Console.Write(i == 0 ? "　" : ""); //中文全角的空格符
                Console.BackgroundColor = qrCodeData.ModuleMatrix[i][j]
                    ? ConsoleColor.Black
                    : ConsoleColor.White;
                Console.Write('　'); //中文全角的空格符
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write(i == qrCodeData.ModuleMatrix.Count - 1 ? "　" : ""); //中文全角的空格符
            }
            Console.WriteLine();
        }
        for (int i = 0; i < qrCodeData.ModuleMatrix.Count + 2; i++)
            Console.Write("　"); //中文全角的空格符

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
            { "11", ' ' },
            { "00", '█' },
            { "10", '▄' },
            { "01", '▀' }, //todo:win平台的cmd会显示？,是已知问题，待想办法解决
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

        logger.LogInformation(Environment.NewLine + string.Join(Environment.NewLine, list));
    }

    private string GetOnlinePic(string str)
    {
        var encode = System.Web.HttpUtility.UrlEncode(str);
        ;
        return $"https://tool.lu/qrcode/basic.html?text={encode}";
    }

    private string GetCookieStr(IEnumerable<string> setCookies)
    {
        var ckItemList = new List<string>();
        foreach (var item in setCookies)
        {
            ckItemList.Add(item.Split(';').FirstOrDefault());
        }
        var biliCk = string.Join("; ", ckItemList);
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

        var qlDir = configuration["QL_DIR"] ?? "/ql";

        string authFile = qlDir;
        if (hostingEnvironment.ContentRootPath.Contains($"{qlDir}/data/"))
        {
            authFile = Path.Combine(authFile, "data");
        }
        authFile = Path.Combine(authFile, "config/auth.json");

        if (!File.Exists(authFile))
        {
            logger.LogWarning("获取青龙授权失败，文件不存在：{authFile}", authFile);
            return token;
        }

        var authJson = await File.ReadAllTextAsync(authFile);

        var jb = JsonConvert.DeserializeObject<JObject>(authJson);
        token = jb["token"]?.ToString();

        return token;
    }

    private Task PrintIfSaveCookieFailAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
    {
        logger.LogError("持久化失败，青龙版本高于2.18，请手动添加环境变量到青龙");
        logger.LogWarning("变量Key：{key}", "Ray_BiliBiliCookies__0");
        logger.LogWarning("变量值：{value}", ckInfo.CookieStr);
        logger.LogWarning(
            "如果Key已存在，请自行+1，如Ray_BiliBiliCookies__1，Ray_BiliBiliCookies__2..."
        );
        return Task.CompletedTask;
    }

    #endregion

    #endregion
}
