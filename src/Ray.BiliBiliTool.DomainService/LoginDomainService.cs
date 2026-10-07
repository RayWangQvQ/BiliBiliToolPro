using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QRCoder;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Agent.Baihu.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.PassportApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.DaiDai;
using Ray.BiliBiliTool.Agent.DaiDai.Dtos;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService.Dtos;
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
    IBaihuApi baihuApi,
    IDaiDaiApi daiDaiApi,
    IHomeApi homeApi,
    IConfiguration configuration,
    IOptions<QingLongOptions> qingLongOptions,
    IOptions<BaihuOptions> baihuOptions,
    IOptions<DaiDaiOptions> daiDaiOptions
) : ILoginDomainService
{
    public async Task<BiliCookie> LoginByQrCodeAsync(CancellationToken cancellationToken)
    {
        BiliCookie? cookieInfo = null;

        var re = await passportApi.GenerateQrCode();
        if (re.Code != 0 || re.Data is null)
        {
            throw new BiliBusinessException($"获取二维码失败：{re.ToJsonStr()}");
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
            if (content?.Code != 0 || content.Data is null)
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

        if (cookieInfo == null)
        {
            throw new BiliIntegrationException("登录超时");
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

    private static readonly SemaphoreSlim CookieJsonSaveLock = new(1, 1);

    public async Task SaveCookieToJsonFileAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        var accountId = GetCookieAccountId(ckInfo);
        await CookieJsonSaveLock.WaitAsync(cancellationToken);
        try
        {
            var directory = new DirectoryInfo(hostingEnvironment.ContentRootPath);
            for (var current = directory; current.Parent is not null; current = current.Parent)
            {
                if (
                    current.Name.Equals(
                        "bin",
                        OperatingSystem.IsWindows()
                            ? StringComparison.OrdinalIgnoreCase
                            : StringComparison.Ordinal
                    )
                )
                {
                    directory = current.Parent;
                    break;
                }
            }
            var path = directory.FullName;
            if (
                string.Equals(
                    configuration["PlatformType"],
                    "Web",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                path = Path.Combine(path, "config");
            Directory.CreateDirectory(path);
            var file = Path.Combine(path, "cookies.json");
            JsonObject document;
            try
            {
                document = File.Exists(file)
                    ? JsonNode.Parse(
                        await File.ReadAllTextAsync(file, cancellationToken),
                        documentOptions: new System.Text.Json.JsonDocumentOptions
                        {
                            CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                            AllowTrailingCommas = true,
                        }
                    ) as JsonObject
                        ?? throw new BiliValidationException("cookies.json 必须是 JSON 对象。")
                    : new JsonObject();
            }
            catch (System.Text.Json.JsonException)
            {
                throw new BiliValidationException("cookies.json 格式错误，请修正后重新保存。");
            }
            var value = document["BiliBiliCookies"];
            if (value is not null && value is not JsonArray)
                throw new BiliValidationException("cookies.json 中 BiliBiliCookies 必须是列表。");
            var cookies = value as JsonArray ?? new JsonArray();
            if (value is null)
                document["BiliBiliCookies"] = cookies;
            var index = -1;
            for (var position = 0; position < cookies.Count; position++)
            {
                if (
                    cookies[position] is JsonValue stored
                    && stored.TryGetValue<string>(out var existingCookie)
                    && MatchesCookieAccount(existingCookie, accountId)
                )
                {
                    index = position;
                    break;
                }
            }
            if (index < 0)
                cookies.Add(ckInfo.CookieStr);
            else
                cookies[index] = ckInfo.CookieStr;

            // Replace only after a complete write, preserving existing file permissions.
            var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(
                    temporary,
                    document.ToJsonString(
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    ),
                    cancellationToken
                );
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(
                        temporary,
                        File.Exists(file)
                            ? File.GetUnixFileMode(file)
                            : UnixFileMode.UserRead | UnixFileMode.UserWrite
                    );
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(file))
                    File.Replace(temporary, file, null);
                else
                    File.Move(temporary, file);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            logger.LogInformation(index < 0 ? "新增成功！" : "更新成功！");
        }
        finally
        {
            CookieJsonSaveLock.Release();
        }
    }

    public async Task<bool> SaveCookieToQinLongAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var accountId = GetCookieAccountId(ckInfo);
            var token = await GetQingLongAuthTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new BiliIntegrationException("获取青龙token失败");
            }

            var qlEnvList = await qingLongApi.GetEnvsAsync("Ray_BiliBiliCookies__", token);
            if (qlEnvList.Code != 200 || qlEnvList.Data is null)
            {
                logger.LogWarning("青龙环境变量查询未成功，状态码：{code}", qlEnvList.Code);
                throw new BiliIntegrationException("青龙未返回环境变量列表");
            }

            var list = qlEnvList
                .Data.Where(x =>
                    x.name?.StartsWith("Ray_BiliBiliCookies__", StringComparison.Ordinal) == true
                )
                .ToList();
            var oldEnv = list.FirstOrDefault(x => MatchesCookieAccount(x.value, accountId));

            if (oldEnv != null)
            {
                logger.LogInformation("用户已存在，更新cookie");
                logger.LogInformation("Key：{key}", oldEnv.name);
                var update = new UpdateQingLongEnv
                {
                    id = oldEnv.id,
                    name = oldEnv.name,
                    value = ckInfo.CookieStr,
                    remarks = string.IsNullOrEmpty(oldEnv.remarks)
                        ? $"bili-{ckInfo.UserId}"
                        : oldEnv.remarks,
                };

                var updateRe = await qingLongApi.UpdateEnvsAsync(update, token);
                if (
                    updateRe.Code != 200
                    || updateRe.Data is null
                    || updateRe.Data.id != update.id
                    || updateRe.Data.name != update.name
                    || updateRe.Data.value != update.value
                )
                {
                    logger.LogWarning("青龙环境变量更新未确认，状态码：{code}", updateRe.Code);
                    throw new BiliIntegrationException("青龙未确认环境变量更新");
                }
                logger.LogInformation("更新成功！");

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
            var addRe = await qingLongApi.AddEnvsAsync([add], token);
            if (
                addRe.Code != 200
                || addRe.Data is null
                || !addRe.Data.Any(item => item.name == add.name && item.value == add.value)
            )
            {
                logger.LogWarning("青龙环境变量新增未确认，状态码：{code}", addRe.Code);
                throw new BiliIntegrationException("青龙未确认环境变量新增");
            }
            logger.LogInformation("新增成功！");
            return true;
        }
        catch
        {
            await PrintIfSaveCookieFailAsync(ckInfo, cancellationToken);
            return false;
        }
    }

    public async Task<QrLoginGenerateResult> GenerateQrCodeWebAsync(
        CancellationToken cancellationToken
    )
    {
        var re = await passportApi.GenerateQrCode();
        if (re.Code != 0 || re.Data is null)
        {
            throw new BiliBusinessException($"获取二维码失败：{re.ToJsonStr()}");
        }

        var url = re.Data.Url;

        // Generate PNG QR code (no console output, per D-01)
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.L);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = pngQrCode.GetGraphic(20);
        var base64 = Convert.ToBase64String(pngBytes);

        var onlineUrl = GetOnlinePic(url);

        return new QrLoginGenerateResult
        {
            QrImageBase64 = base64,
            QrcodeKey = re.Data.Qrcode_key,
            OnlineUrl = onlineUrl,
        };
    }

    public async Task<QrLoginCheckResult> CheckQrLoginAsync(
        string qrcodeKey,
        CancellationToken cancellationToken
    )
    {
        var check = await passportApi.CheckQrCodeHasScaned(qrcodeKey);
        if (!check.IsSuccessStatusCode)
        {
            return new QrLoginCheckResult
            {
                Status = QrLoginStatus.Error,
                Message = "检测接口异常",
            };
        }

        var contentStr = await check.Content.ReadAsStringAsync(cancellationToken);
        var content = JsonConvert.DeserializeObject<BiliApiResponse<TokenDto>>(contentStr);
        if (content?.Code != 0 || content.Data is null)
        {
            return new QrLoginCheckResult
            {
                Status = QrLoginStatus.Error,
                Message = $"检测接口异常：{check.ToJsonStr()}",
            };
        }

        if (content.Data.Code == 86038) // 已失效
        {
            return new QrLoginCheckResult
            {
                Status = QrLoginStatus.Expired,
                Message = content.Data.Message,
            };
        }

        if (content.Data.Code == 0) // 扫描成功
        {
            IEnumerable<string> cookies = check
                .Headers.SingleOrDefault(header => header.Key == "Set-Cookie")
                .Value;

            var cookieStr = CookieInfo.ConvertSetCkHeadersToCkStr(cookies);
            var cookieInfo = CookieStrFactory<BiliCookie>.CreateNew(cookieStr);
            cookieInfo.Check();

            return new QrLoginCheckResult { Status = QrLoginStatus.Success, Cookie = cookieInfo };
        }

        return new QrLoginCheckResult
        {
            Status = QrLoginStatus.Waiting,
            Message = content.Data.Message,
        };
    }

    public async Task<bool> SaveCookieToBaihuAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var accountId = GetCookieAccountId(ckInfo);
            var token = baihuOptions.Value.Token;
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("未配置白虎API Token");
            }

            if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = $"Bearer {token}";
            }

            var envListRe = await baihuApi.GetEnvsAsync(token);
            if (envListRe.Code != 200)
            {
                throw new Exception($"查询白虎环境变量失败：{envListRe.Msg}");
            }

            var list = envListRe
                .Data.Where(x =>
                    x.Name?.StartsWith("Ray_BiliBiliCookies__", StringComparison.Ordinal) == true
                )
                .ToList();
            var oldEnv = list.FirstOrDefault(x => MatchesCookieAccount(x.Value, accountId));

            if (oldEnv != null)
            {
                logger.LogInformation("用户已存在，更新白虎环境变量");
                logger.LogInformation("Key：{key}", oldEnv.Name);

                oldEnv.Value = ckInfo.CookieStr;
                oldEnv.Remark = string.IsNullOrEmpty(oldEnv.Remark)
                    ? $"bili-{ckInfo.UserId}"
                    : oldEnv.Remark;

                var updateRe = await baihuApi.UpdateEnvAsync(oldEnv.Id, oldEnv, token);
                logger.LogInformation(updateRe.Code == 200 ? "更新成功！" : updateRe.Msg);

                return true;
            }

            logger.LogInformation("用户不存在，新增白虎环境变量");
            var maxNum = -1;
            if (list.Any())
            {
                maxNum = list.Select(x =>
                    {
                        var num = x.Name.Replace("Ray_BiliBiliCookies__", "");
                        var parseSuc = int.TryParse(num, out int envNum);
                        return parseSuc ? envNum : 0;
                    })
                    .Max();
            }

            var name = $"Ray_BiliBiliCookies__{maxNum + 1}";
            logger.LogInformation("Key：{key}", name);

            var add = new BaihuEnv
            {
                Name = name,
                Value = ckInfo.CookieStr,
                Remark = $"bili-{ckInfo.UserId}",
                Enabled = true,
            };
            var addRe = await baihuApi.AddEnvAsync(add, token);
            logger.LogInformation(addRe.Code == 200 ? "新增成功！" : addRe.Msg);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError("保存到白虎失败：{msg}", e.Message);
            await PrintIfSaveCookieFailAsync(ckInfo, cancellationToken);
            return false;
        }
    }

    public async Task<bool> SaveCookieToDaiDaiAsync(
        BiliCookie ckInfo,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var accountId = GetCookieAccountId(ckInfo);
            // 先用 AppKey/AppSecret 换取 access_token
            var token = await GetDaiDaiAuthTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("获取呆呆面板token失败");
            }

            // all=1 返回全部匹配项，避免分页漏掉
            var envListRe = await daiDaiApi.GetEnvsAsync("Ray_BiliBiliCookies__", "1", token);

            var list = (envListRe.Data ?? [])
                .Where(x =>
                    x.Name != null
                    && x.Name?.StartsWith("Ray_BiliBiliCookies__", StringComparison.Ordinal) == true
                )
                .ToList();
            var oldEnv = list.FirstOrDefault(x =>
                x.Value != null && MatchesCookieAccount(x.Value, accountId)
            );

            if (oldEnv != null)
            {
                logger.LogInformation("用户已存在，更新呆呆面板环境变量");
                logger.LogInformation("Key：{key}", oldEnv.Name);

                oldEnv.Value = ckInfo.CookieStr;
                oldEnv.Remarks = string.IsNullOrEmpty(oldEnv.Remarks)
                    ? $"bili-{ckInfo.UserId}"
                    : oldEnv.Remarks;

                var updateRe = await daiDaiApi.UpdateEnvAsync(oldEnv.Id, oldEnv, token);
                logger.LogInformation("更新成功！{msg}", updateRe.Message);

                return true;
            }

            logger.LogInformation("用户不存在，新增呆呆面板环境变量");
            var maxNum = -1;
            if (list.Any())
            {
                maxNum = list.Select(x =>
                    {
                        var num = (x.Name ?? "").Replace("Ray_BiliBiliCookies__", "");
                        var parseSuc = int.TryParse(num, out int envNum);
                        return parseSuc ? envNum : 0;
                    })
                    .Max();
            }

            var name = $"Ray_BiliBiliCookies__{maxNum + 1}";
            logger.LogInformation("Key：{key}", name);

            var add = new DaiDaiEnv
            {
                Name = name,
                Value = ckInfo.CookieStr,
                Remarks = $"bili-{ckInfo.UserId}",
                Enabled = true,
            };
            var addRe = await daiDaiApi.AddEnvAsync(add, token);
            logger.LogInformation("新增成功！{msg}", addRe.Message);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError("保存到呆呆面板失败：{msg}", e.Message);
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
        return $"https://tool.lu/qrcode/basic.html?text={encode}";
    }

    private static long GetCookieAccountId(BiliCookie cookie)
    {
        if (
            !long.TryParse(
                cookie.UserId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var accountId
            )
            || accountId <= 0
        )
            throw new BiliValidationException("Cookie 缺少有效的 DedeUserID，无法保存。");
        return accountId;
    }

    private static bool MatchesCookieAccount(string? value, long accountId)
    {
        var existing = CookieStrFactory<BiliCookie>.CreateNew(value ?? "");
        return long.TryParse(
                existing.UserId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var existingId
            )
            && existingId == accountId;
    }

    #region qinglong

    private async Task<string> GetQingLongAuthTokenAsync()
    {
        logger.LogWarning("使用OpenAPI鉴权");
        if (
            string.IsNullOrWhiteSpace(qingLongOptions.Value.ClientId)
            || string.IsNullOrWhiteSpace(qingLongOptions.Value.ClientSecret)
        )
        {
            logger.LogWarning("未配置青龙的ClientId和ClientSecret，无法自动获取token");
            logger.LogWarning(
                "教程：{qingDoc}",
                "https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/platforms/qinglong/README.md"
            );
            return "";
        }

        var token = await qingLongApi.GetTokenAsync(
            qingLongOptions.Value.ClientId!,
            qingLongOptions.Value.ClientSecret!
        );

        if (token.Code != 200)
        {
            logger.LogWarning("青龙 OpenAPI 鉴权失败，状态码：{code}", token.Code);
            return "";
        }
        if (
            token.Data is null
            || string.IsNullOrWhiteSpace(token.Data.token_type)
            || string.IsNullOrWhiteSpace(token.Data.token)
        )
        {
            logger.LogWarning("青龙 OpenAPI 未返回完整授权信息");
            return "";
        }
        return $"{token.Data.token_type} {token.Data.token}";
    }

    private async Task<string> GetDaiDaiAuthTokenAsync()
    {
        logger.LogWarning("使用呆呆面板OpenAPI鉴权");
        if (
            string.IsNullOrWhiteSpace(daiDaiOptions.Value.AppKey)
            || string.IsNullOrWhiteSpace(daiDaiOptions.Value.AppSecret)
        )
        {
            logger.LogWarning("未配置呆呆面板的AppKey和AppSecret，无法自动获取token");
            logger.LogWarning(
                "教程：{daidaiDoc}",
                "https://github.com/RayWangQvQ/BiliBiliToolPro/blob/develop/platforms/daidai/README.md"
            );
            return "";
        }

        var re = await daiDaiApi.GetTokenAsync(
            new DaiDaiTokenRequest
            {
                AppKey = daiDaiOptions.Value.AppKey,
                AppSecret = daiDaiOptions.Value.AppSecret,
            }
        );

        if (re.Data == null || string.IsNullOrEmpty(re.Data.AccessToken))
        {
            return "";
        }

        return $"{re.Data.TokenType} {re.Data.AccessToken}";
    }

    private Task PrintIfSaveCookieFailAsync(BiliCookie ckInfo, CancellationToken cancellationToken)
    {
        var platform = configuration["Ray_PlatformType"] ?? "";
        var platformName =
            platform.Equals("Baihu", StringComparison.OrdinalIgnoreCase) ? "白虎"
            : platform.Equals("DaiDai", StringComparison.OrdinalIgnoreCase) ? "呆呆"
            : "青龙";

        if (platformName == "白虎")
        {
            logger.LogError("持久化失败，请手动添加环境变量到白虎面板");
            logger.LogInformation(
                "提示：配置环境变量 BaihuConfig__Token 后，在baihu面板系统设置->openapi获取，程序可尝试自动保存。"
            );
        }
        else if (platformName == "呆呆")
        {
            logger.LogError("持久化失败，请手动添加环境变量到呆呆面板");
            logger.LogInformation(
                "提示：在呆呆面板「系统设置->Open API」新建应用（授权范围含 envs），配置环境变量 DaiDaiConfig__AppKey / DaiDaiConfig__AppSecret 后，程序可尝试自动保存。"
            );
        }
        else
        {
            logger.LogError(
                "保存到青龙失败，请检查 OpenAPI 地址、应用权限及 ClientId／ClientSecret 配置。"
            );
        }

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
