using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

/// <summary>
/// 防爬
/// </summary>
public class WbiService(ILogger<WbiService> logger, IUserInfoApi userInfoApi) : IWbiService
{
    private Dictionary<BiliCookie, WbiImg> _cache = new();

    public async Task<WridDto> GetWridAsync(Dictionary<string, string> parameters, BiliCookie ck)
    {
        parameters.Remove(nameof(IWrid.wts));
        parameters.Remove(nameof(IWrid.w_rid));

        WbiImg wbi = await GetWbiKeysAsync(ck);

        return EncWbi(parameters, wbi.ImgKey, wbi.SubKey);
    }

    public async Task SetWridAsync<T>(T request, BiliCookie ck)
        where T : IWrid
    {
        //生成字典
        Dictionary<string, string> parameters = ObjectHelper
            .ObjectToDictionary(request)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "");

        //生成
        var re = await GetWridAsync(parameters, ck);

        request.w_rid = re.w_rid;
        request.wts = re.wts;
    }

    /// <summary>
    /// 为请求参数进行 wbi 签名
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="imgKey"></param>
    /// <param name="subKey"></param>
    /// <param name="timespan"></param>
    /// <returns></returns>
    public WridDto EncWbi(
        Dictionary<string, string> parameters,
        string imgKey,
        string subKey,
        long timespan = 0
    )
    {
        var re = new WridDto();

        var mixinKey = GetMixinKey(imgKey + subKey);

        if (timespan == 0)
        {
            re.wts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }
        else
        {
            re.wts = timespan;
        }

        var chrFilter = new Regex("[!'()*]");

        var dic = new Dictionary<string, string> { { "wts", re.wts.ToString() } };

        foreach (var entry in parameters)
        {
            var key = entry.Key;
            var value = entry.Value;

            var encodedValue = chrFilter.Replace(value, "");

            dic.Add(Uri.EscapeDataString(key), Uri.EscapeDataString(encodedValue));
        }

        var keyList = dic.Keys.ToList();
        keyList.Sort();

        var queryList = (
            from item in keyList
            let value = dic[item]
            select $"{item}={value}"
        ).ToList();

        var queryString = string.Join("&", queryList);
        var hashStr = queryString + mixinKey;
        var hashedQueryString = MD5.HashData(Encoding.UTF8.GetBytes(hashStr));
        var wbiSign = BitConverter.ToString(hashedQueryString).Replace("-", "").ToLower();

        re.w_rid = wbiSign;

        return re;
    }

    private async Task<WbiImg> GetWbiKeysAsync(BiliCookie ck)
    {
        _cache.TryGetValue(ck, out var wbiImg);

        if (wbiImg != null)
            return wbiImg;

        BiliApiResponse<UserInfo> apiResponse = await userInfoApi.LoginByCookie(ck.ToString());
        UserInfo useInfo = apiResponse.Data;
        logger.LogDebug("【img_url】{0}", useInfo.Wbi_img?.img_url);
        logger.LogDebug("【sub_url】{0}", useInfo.Wbi_img?.sub_url);
        wbiImg = useInfo.Wbi_img;
        _cache[ck] = wbiImg;
        return wbiImg;
    }

    /// <summary>
    /// 对 imgKey 和 subKey 进行字符顺序打乱编码
    /// </summary>
    /// <param name="orig"></param>
    /// <returns></returns>
    private string GetMixinKey(string orig)
    {
        int[] mixinKeyEncTab = new int[]
        {
            46,
            47,
            18,
            2,
            53,
            8,
            23,
            32,
            15,
            50,
            10,
            31,
            58,
            3,
            45,
            35,
            27,
            43,
            5,
            49,
            33,
            9,
            42,
            19,
            29,
            28,
            14,
            39,
            12,
            38,
            41,
            13,
            37,
            48,
            7,
            16,
            24,
            55,
            40,
            61,
            26,
            17,
            0,
            1,
            60,
            51,
            30,
            4,
            22,
            25,
            54,
            21,
            56,
            59,
            6,
            63,
            57,
            62,
            11,
            36,
            20,
            34,
            44,
            52,
        };

        var temp = new StringBuilder();
        foreach (var index in mixinKeyEncTab)
        {
            temp.Append(orig[index]);
        }
        return temp.ToString().Substring(0, 32);
    }
}

public class WridDto : IWrid
{
    public long wts { get; set; }

    public string w_rid { get; set; }
}

public interface IWrid
{
    public long wts { get; set; }

    public string w_rid { get; set; }
}
