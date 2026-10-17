using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Agent.Baihu.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
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
    IGaiaApi gaiaApi,
    IConfiguration configuration,
    IOptions<QingLongOptions> qingLongOptions,
    IOptions<BaihuOptions> baihuOptions,
    IOptions<DaiDaiOptions> daiDaiOptions,
    IOptions<DeviceCookieOptions> deviceCookieOptions
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

                if (ApplyPinnedDeviceCookie(biliCookie))
                {
                    // Pinned device cookies configured: never refresh nor re-enroll.
                }
                else
                {
                    await EnsureDeviceCookieAsync(biliCookie, cancellationToken);
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

    /// <summary>
    /// Applies pinned device cookies from configuration when present.
    /// </summary>
    /// <returns>true when any value was applied; the caller must then skip enrollment.</returns>
    private bool ApplyPinnedDeviceCookie(BiliCookie biliCookie)
    {
        var options = deviceCookieOptions.Value;
        if (!options.HasPinnedValues)
            return false;

        var applied = 0;
        if (!string.IsNullOrWhiteSpace(options.Buvid3))
        {
            biliCookie.CookieItemDictionary["buvid3"] = options.Buvid3;
            applied++;
        }

        if (!string.IsNullOrWhiteSpace(options.Buvid4))
        {
            biliCookie.CookieItemDictionary["buvid4"] = options.Buvid4;
            applied++;
        }

        if (!string.IsNullOrWhiteSpace(options.BNut))
        {
            biliCookie.CookieItemDictionary["b_nut"] = options.BNut;
            applied++;
        }

        logger.LogInformation(
            "已应用固定设备指纹配置，覆盖{count}项设备Cookie，本次不再刷新服务端指纹",
            applied
        );
        return true;
    }

    /// <summary>
    /// Ensures the current device has a server-side profile.
    /// </summary>
    /// <remarks>
    /// A buvid3 generated by the program (finger/spi) has no profile, so share and other
    /// endpoints reject it with -403 "account abnormal"; the device has to be reported once
    /// to the gaia gateway (ExClimbWuzhi) to create the profile.
    /// The presence of _uuid in the cookie is used as the "already enrolled" marker to avoid
    /// repeated reports; an enrolled device stays stable and is never refreshed daily,
    /// because refreshing loses the profile and triggers risk control.
    /// Verified against the production container on 2026-09-15.
    /// </remarks>
    private async Task EnsureDeviceCookieAsync(
        BiliCookie biliCookie,
        CancellationToken cancellationToken
    )
    {
        var items = biliCookie.CookieItemDictionary;
        var hasBuvid3 =
            items.TryGetValue("buvid3", out var buvid3) && !string.IsNullOrWhiteSpace(buvid3);
        var hasUuid = items.TryGetValue("_uuid", out var uuid) && !string.IsNullOrWhiteSpace(uuid);

        if (hasBuvid3 && hasUuid)
        {
            logger.LogInformation("设备指纹已激活（buvid3 与 _uuid 齐全），保持稳定不刷新");
            return;
        }

        if (!hasBuvid3)
        {
            logger.LogInformation("设备Cookie缺少buvid3，通过finger/spi生成");
            await RefreshDeviceFingerprintAsync(biliCookie);
            hasBuvid3 =
                items.TryGetValue("buvid3", out buvid3) && !string.IsNullOrWhiteSpace(buvid3);
            if (!hasBuvid3)
            {
                logger.LogWarning("设备指纹生成失败，跳过gaia上报");
                return;
            }
        }

        var deviceUuid = hasUuid ? uuid! : GenerateDeviceUuid();
        try
        {
            var payload = BuildGaiaPayload(deviceUuid);
            var response = await gaiaApi.ReportDeviceFingerprint(
                BuildDeviceCookieHeader(biliCookie, deviceUuid),
                new GaiaReportRequest { Payload = payload }
            );
            if (response?.Code == 0)
            {
                items["_uuid"] = deviceUuid;
                logger.LogInformation("设备指纹上报成功，服务端设备档案已建立");
            }
            else
            {
                logger.LogWarning(
                    "设备指纹上报失败，返回码：{code}，消息：{message}",
                    response?.Code,
                    response?.Message
                );
            }
        }
        catch (Exception e)
        {
            logger.LogWarning("设备指纹上报异常：{msg}", e.Message);
        }
    }

    /// <summary>
    /// Common real screen profiles (width, height, available height) used to perturb the fingerprint
    /// </summary>
    private static readonly (int Width, int Height, int AvailHeight)[] ScreenProfiles =
    [
        (1920, 1080, 1048),
        (1600, 900, 868),
        (1536, 864, 832),
        (1440, 900, 868),
        (2560, 1440, 1408),
    ];

    /// <summary>
    /// Builds the ExClimbWuzhi payload from the real browser device template, replacing the
    /// dynamic fields (timestamp 5062, referer 03bf, spm 39c8, device _uuid df35) and
    /// perturbing screen resolution and canvas tails so multiple devices do not share an
    /// identical fingerprint.
    /// </summary>
    private static string BuildGaiaPayload(string deviceUuid)
    {
        var payload = JObject.Parse(GaiaDeviceFingerprintTemplate.PayloadJson);
        payload["5062"] = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
        payload["03bf"] = "https%3A%2F%2Fwww.bilibili.com%2F";
        payload["39c8"] = "333.1007.fp.risk";
        payload["df35"] = deviceUuid;

        var fingerprint = (JObject)payload["3c43"]!;
        var (width, height, availHeight) = ScreenProfiles[
            Random.Shared.Next(ScreenProfiles.Length)
        ];
        fingerprint["748e"] = new JArray(width, height);
        fingerprint["d61f"] = new JArray(width, availHeight);
        fingerprint["13ab"] = PerturbCanvasTail((string)fingerprint["13ab"]!);
        fingerprint["bfe9"] = PerturbCanvasTail((string)fingerprint["bfe9"]!);

        return payload.ToString(Formatting.None);
    }

    /// <summary>
    /// Randomizes the base64 tail of a canvas fingerprint while keeping it well-formed
    /// </summary>
    private static string PerturbCanvasTail(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        var chars = value.ToCharArray();
        var start = Math.Max(0, chars.Length - 16);
        for (var i = start; i < chars.Length; i++)
        {
            if (chars[i] != '=')
            {
                chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Generates a browser-like _uuid: 9-4-5-4-12 uppercase hex digits plus 6 digits and the "infoc" suffix
    /// </summary>
    private static string GenerateDeviceUuid()
    {
        var hex = Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
        return $"{hex[..9]}-{hex[9..13]}-{hex[13..18]}-{hex[18..22]}-{hex[22..34]}"
            + $"{Random.Shared.Next(100000, 999999)}infoc";
    }

    /// <summary>
    /// Builds the device cookie header (buvid3/buvid4/b_nut/_uuid) required by the gaia report
    /// </summary>
    private static string BuildDeviceCookieHeader(BiliCookie biliCookie, string deviceUuid)
    {
        var items = biliCookie.CookieItemDictionary;
        var parts = new List<string>();
        foreach (var key in new[] { "buvid3", "buvid4", "b_nut" })
        {
            if (items.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={value}");
            }
        }

        parts.Add($"_uuid={deviceUuid}");
        return string.Join("; ", parts);
    }

    private async Task RefreshDeviceFingerprintAsync(BiliCookie biliCookie)
    {
        var response = await gaiaApi.GetDeviceFingerprint(biliCookie.ToString());
        if (response?.Code != 0 || response.Data is null)
        {
            logger.LogWarning(
                "刷新设备指纹失败，保留现有Cookie，返回码：{code}，消息：{message}",
                response?.Code,
                response?.Message
            );
            return;
        }

        var updated = 0;
        if (!string.IsNullOrWhiteSpace(response.Data.B_3))
        {
            biliCookie.CookieItemDictionary["buvid3"] = response.Data.B_3;
            updated++;
        }

        if (!string.IsNullOrWhiteSpace(response.Data.B_4))
        {
            biliCookie.CookieItemDictionary["buvid4"] = response.Data.B_4;
            updated++;
        }

        logger.LogInformation("设备指纹刷新成功，更新{count}项设备Cookie", updated);
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
            await using var stream = File.Create(fileInfo.PhysicalPath!);
            await using var sw = new StreamWriter(stream);
            await sw.WriteAsync($"{{{Environment.NewLine}}}");
        }

        string json;
        await using (var stream = new FileStream(fileInfo.PhysicalPath!, FileMode.Open))
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

        ckInfo.CookieItemDictionary.TryGetValue("DedeUserID", out var userId);
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
            if (string.IsNullOrEmpty(token))
            {
                throw new BiliIntegrationException("获取青龙token失败");
            }

            var qlEnvList = await qingLongApi.GetEnvsAsync("Ray_BiliBiliCookies__", token);
            if (qlEnvList.Code != 200)
            {
                throw new BiliIntegrationException($"查询环境变量失败：{qlEnvList.ToJsonStr()}");
            }

            logger.LogDebug(qlEnvList.Data.ToJsonStr());
            logger.LogDebug(ckInfo.ToString());

            var list = qlEnvList
                .Data.Where(x => x.name.StartsWith("Ray_BiliBiliCookies__"))
                .ToList();
            var oldEnv = list.FirstOrDefault(x => x.value.Contains(ckInfo.UserId));

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
            var addRe = await qingLongApi.AddEnvsAsync([add], token);
            logger.LogInformation(addRe.Code == 200 ? "新增成功！" : addRe.ToJsonStr());
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
                .Data.Where(x => x.Name.StartsWith("Ray_BiliBiliCookies__"))
                .ToList();
            var oldEnv = list.FirstOrDefault(x => x.Value.Contains(ckInfo.UserId));

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
            // 先用 AppKey/AppSecret 换取 access_token
            var token = await GetDaiDaiAuthTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("获取呆呆面板token失败");
            }

            // all=1 返回全部匹配项，避免分页漏掉
            var envListRe = await daiDaiApi.GetEnvsAsync("Ray_BiliBiliCookies__", "1", token);

            var list = (envListRe.Data ?? [])
                .Where(x => x.Name != null && x.Name.StartsWith("Ray_BiliBiliCookies__"))
                .ToList();
            var oldEnv = list.FirstOrDefault(x =>
                x.Value != null && x.Value.Contains(ckInfo.UserId)
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

    private async Task SaveJson(List<string> lines, IFileInfo fileInfo)
    {
        var newJson = string.Join(Environment.NewLine, lines);

        await using var sw = new StreamWriter(fileInfo.PhysicalPath!);
        await sw.WriteAsync(newJson);
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
            logger.LogError("持久化失败，青龙版本高于2.18，请手动添加环境变量到青龙");
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
